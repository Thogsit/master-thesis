using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using OpenFga.Sdk.Client.Model;
using SealedFga.Attributes;
using SealedFga.AuthModel;

namespace SealedFga.Fga;

/// <summary>
///     Due to the restriction to netstandard 2.0, this class is used to decouple the logic from the
///     SealedFgaSaveChangesInterceptor class.
///     The SealedFgaSaveChangesInterceptor.partial.g.cs file contains the inheritance and uses the below methods.
/// </summary>
public class SealedFgaSaveChangesProcessor(IServiceProvider serviceProvider) {
    /// <summary>
    ///     Main method that processes SealedFGA changes for a given DbContext.
    /// </summary>
    /// <param name="context">The DbContext to process changes for.</param>
    public void ProcessSealedFgaChanges(DbContext? context) {
        if (context is null) {
            return;
        }

        // Filters the change tracker for all entries that can possibly change OpenFGA relations
        var relevantEntries = context.ChangeTracker
                                     .Entries()
                                     .Where(e => e.State
                                                     is EntityState.Deleted
                                                        or EntityState.Modified
                                                        or EntityState.Added
                                                 && e.Entity.GetType().GetInterfaces()
                                                     .Any(i =>
                                                          i.IsGenericType && i.GetGenericTypeDefinition() ==
                                                          typeof(ISealedFgaType<>)
                                                      )
                                      );

        // Retrieve SealedFGA relations to write/delete
        var writeRelations = new List<ClientTupleKey>();
        var deleteRelations = new List<ClientTupleKeyWithoutCondition>();
        foreach (var entry in relevantEntries) {
            ProcessSingleEntityEntry(entry, ref writeRelations, ref deleteRelations);
        }

        if (deleteRelations.Count <= 0 && writeRelations.Count <= 0) {
            return;
        }

        // Queue the relations to be written/deleted in OpenFGA
        var sealedFgaService = serviceProvider.GetRequiredService<SealedFgaService>();
        _ = sealedFgaService.QueueDeletes(
            deleteRelations.Select(dr => (
                    dr.User,
                    dr.Relation,
                    dr.Object
                )
            )
        );
        _ = sealedFgaService.QueueWrites(
            writeRelations.Select(wr => (
                    wr.User,
                    wr.Relation,
                    wr.Object
                )
            )
        );
    }

    /// <summary>
    ///     Processes a single entity entry to extract OpenFGA relations.
    /// </summary>
    /// <param name="entry">The entity entry to process.</param>
    /// <param name="writeRelations">List to populate with relations to write.</param>
    /// <param name="deleteRelations">List to populate with relations to delete.</param>
    private static void ProcessSingleEntityEntry(
        EntityEntry entry,
        ref List<ClientTupleKey> writeRelations,
        ref List<ClientTupleKeyWithoutCondition> deleteRelations
    ) {
        var entityType = entry.Entity.GetType();
        var entityIdProperty = entry.Property(nameof(ISealedFgaType<>.Id));

        foreach (var property in entityType
                                .GetProperties()
                                .Where(prop => prop.GetCustomAttribute<SealedFgaRelationAttribute>() != null)) {
            var attr = property.GetCustomAttribute<SealedFgaRelationAttribute>()!;
            var entryProperty = entry.Property(property.Name);

            ProcessEntityPropertyRelation(
                entry.State,
                attr,
                entityIdProperty,
                entryProperty,
                ref writeRelations,
                ref deleteRelations
            );
        }
    }

    /// <summary>
    ///     Processes a single property relation for an entity based on its state.
    /// </summary>
    /// <param name="entityState">The state of the entity (Added, Modified, Deleted).</param>
    /// <param name="relationAttribute">The OpenFGA relation attribute.</param>
    /// <param name="entityIdProperty">The entity's ID property.</param>
    /// <param name="relationProperty">The relation property being processed.</param>
    /// <param name="writeRelations">List to populate with relations to write.</param>
    /// <param name="deleteRelations">List to populate with relations to delete.</param>
    private static void ProcessEntityPropertyRelation(
        EntityState entityState,
        SealedFgaRelationAttribute relationAttribute,
        PropertyEntry entityIdProperty,
        PropertyEntry relationProperty,
        ref List<ClientTupleKey> writeRelations,
        ref List<ClientTupleKeyWithoutCondition> deleteRelations
    ) {
        // We're only interested in changes, so disable the "default case missing" warning.
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (entityState) {
            case EntityState.Added:
                ProcessAddedEntity(relationAttribute,
                    entityIdProperty,
                    relationProperty,
                    ref writeRelations
                );
                break;
            case EntityState.Deleted:
                ProcessDeletedEntity(relationAttribute,
                    entityIdProperty,
                    relationProperty,
                    ref deleteRelations
                );
                break;
            case EntityState.Modified:
                ProcessModifiedEntity(relationAttribute,
                    entityIdProperty,
                    relationProperty,
                    ref writeRelations,
                    ref deleteRelations
                );
                break;
        }
    }

    /// <summary>
    ///     Processes relations for a newly added entity.
    /// </summary>
    private static void ProcessAddedEntity(
        SealedFgaRelationAttribute attr,
        PropertyEntry entityIdProperty,
        PropertyEntry relationProperty,
        ref List<ClientTupleKey> writeRelations
    ) {
        var (userTupleStr, objTupleStr) = ExtractTupleStrings(
            attr,
            entityIdProperty.CurrentValue,
            relationProperty.CurrentValue
        );

        writeRelations.Add(new ClientTupleKey {
                User = userTupleStr,
                Relation = attr.Relation,
                Object = objTupleStr,
            }
        );
    }

    /// <summary>
    ///     Processes relations for a deleted entity.
    /// </summary>
    private static void ProcessDeletedEntity(
        SealedFgaRelationAttribute attr,
        PropertyEntry entityIdProperty,
        PropertyEntry relationProperty,
        ref List<ClientTupleKeyWithoutCondition> deleteRelations
    ) {
        var (userTupleStr, objTupleStr) = ExtractTupleStrings(
            attr,
            entityIdProperty.CurrentValue,
            relationProperty.CurrentValue
        );

        deleteRelations.Add(new ClientTupleKeyWithoutCondition {
                User = userTupleStr,
                Relation = attr.Relation,
                Object = objTupleStr,
            }
        );
    }

    /// <summary>
    ///     Processes relations for a modified entity.
    /// </summary>
    private static void ProcessModifiedEntity(
        SealedFgaRelationAttribute attr,
        PropertyEntry entityIdProperty,
        PropertyEntry relationProperty,
        ref List<ClientTupleKey> writeRelations,
        ref List<ClientTupleKeyWithoutCondition> deleteRelations
    ) {
        // Only process if the ID has actually changed
        if (entityIdProperty.CurrentValue == entityIdProperty.OriginalValue
            && relationProperty.CurrentValue == relationProperty.OriginalValue) {
            return;
        }

        // Process previous values (for deletion)
        var (prevUserTupleStr, prevObjTupleStr) = ExtractTupleStrings(
            attr,
            entityIdProperty.OriginalValue,
            relationProperty.OriginalValue
        );

        deleteRelations.Add(new ClientTupleKeyWithoutCondition {
                User = prevUserTupleStr,
                Relation = attr.Relation,
                Object = prevObjTupleStr,
            }
        );

        // Process current values (for writing)
        var (userTupleStr, objTupleStr) = ExtractTupleStrings(
            attr,
            entityIdProperty.CurrentValue,
            relationProperty.CurrentValue
        );

        writeRelations.Add(new ClientTupleKey {
                User = userTupleStr,
                Relation = attr.Relation,
                Object = objTupleStr,
            }
        );
    }

    /// <summary>
    ///     Extracts tuple strings from entity and property values using reflection.
    /// </summary>
    /// <param name="attr">The OpenFGA relation attribute.</param>
    /// <param name="entityIdValue">The entity's ID value.</param>
    /// <param name="propertyValue">The relation property value.</param>
    /// <returns>A tuple containing user and object tuple strings.</returns>
    private static (string userTupleStr, string objTupleStr) ExtractTupleStrings(
        SealedFgaRelationAttribute attr,
        object? entityIdValue,
        object? propertyValue
    ) {
        // Retrieve foreign key object's ID value
        var propertyOpenFgaTupleString = (string) propertyValue!
                                                 .GetType()
                                                 .GetMethod(nameof(ISealedFgaTypeId<>.AsOpenFgaIdTupleString))!
                                                 .Invoke(propertyValue, null)!;

        // Retrieve this entity's OpenFGA ID
        var entityOpenFgaTupleString = (string) entityIdValue!
                                               .GetType()
                                               .GetMethod(nameof(ISealedFgaTypeId<>.AsOpenFgaIdTupleString))!
                                               .Invoke(entityIdValue, null)!;

        // Switch obj <-> user based on the relation's target type
        return attr.TargetType switch {
            SealedFgaRelationTargetType.Object => (propertyOpenFgaTupleString, entityOpenFgaTupleString),
            SealedFgaRelationTargetType.User => (entityOpenFgaTupleString, propertyOpenFgaTupleString),
        };
    }
}
