using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OpenFga.Sdk.Client.Model;
using SealedFga.Attributes;

namespace SealedFga.Sample.StateSync;

/// <summary>
///     Interceptor for DB save actions.
///     For every changed OpenFGA entity, makes sure the changed relations are sent to the OpenFGA service.
/// </summary>
public class OpenFgaSaveChangesInterceptor : SaveChangesInterceptor {
    /// <inheritdoc />
    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = new()
    ) {
        var context = eventData.Context;
        if (context is null) {
            return base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        // Filter for modified entries
        var writeRelations = new List<ClientTupleKey>();
        var deletedRelations = new List<ClientTupleKeyWithoutCondition>();
        foreach (var entry in context.ChangeTracker
                                     .Entries()
                                     .Where(e => e.State
                                                     is EntityState.Deleted
                                                        or EntityState.Modified
                                                        or EntityState.Added
                                                 && e.Entity.GetType().IsAssignableTo(typeof(IOpenFgaType<>))
                                      )) {
            var entityType = entry.Entity.GetType();
            var entityIdProperty = entry.Property(nameof(IOpenFgaType<>.Id));

            // Search for OpenFGA relations in the entity's properties
            foreach (var property in entityType.GetProperties()) {
                var attr = property.GetCustomAttribute<OpenFgaRelationAttribute>();
                if (attr == null) {
                    continue;
                }

                var entryProperty = entry.Property(property.Name);

                // Retrieve foreign key object's ID value
                var propertyOpenFgaTupleString = (string)entryProperty.CurrentValue!
                                                                      .GetType()
                                                                      .GetMethod(
                                                                           nameof(IOpenFgaTypeId<>.AsOpenFgaIdTupleString)
                                                                       )!
                                                                      .Invoke(entryProperty.CurrentValue, null)!;

                // Retrieve this entity's OpenFGA ID
                var entityOpenFgaTupleString = (string)entry.Entity
                                                            .GetType()
                                                            .GetMethod(nameof(IOpenFgaType<>.AsOpenFgaIdTupleString))!
                                                            .Invoke(entry.Entity, null)!;

                // Switch obj <-> user based on the relation's target type
                var (userTupleStr, objTupleStr) = attr.TargetType switch {
                    OpenFgaRelationTargetType.Object => (propertyOpenFgaTupleString, entityOpenFgaTupleString),
                    OpenFgaRelationTargetType.User => (entityOpenFgaTupleString, propertyOpenFgaTupleString),
                };

                // Entity is newly added -> write contained OpenFGA relations
                if (entry.State == EntityState.Added) {
                    writeRelations.Add(
                        new ClientTupleKey {
                            User = userTupleStr,
                            Relation = attr.Relation,
                            Object = objTupleStr,
                        }
                    );
                }

                // Entity is deleted -> remove contained OpenFGA relations
                else if (entry.State == EntityState.Deleted) {
                    deletedRelations.Add(
                        new ClientTupleKeyWithoutCondition {
                            User = userTupleStr,
                            Relation = attr.Relation,
                            Object = objTupleStr,
                        }
                    );
                }

                // Entity is modified -> write current and remove previous values
                else if (entry.State == EntityState.Modified) {
                    var asOpenFgaIdTupleStrMethod = property
                        .PropertyType
                        .GetMethod(nameof(IOpenFgaTypeId<>.AsOpenFgaIdTupleString))!;
                    var curIdTupleStr = (string)asOpenFgaIdTupleStrMethod
                       .Invoke(entityIdProperty.CurrentValue, null)!;
                    var prevIdTupleStr = (string)asOpenFgaIdTupleStrMethod
                       .Invoke(entryProperty.OriginalValue, null)!;

                    // Only do anything if the ID has actually changed
                    if (curIdTupleStr == prevIdTupleStr) {
                        continue;
                    }

                    // Remove previous value
                    deletedRelations.Add(
                        new ClientTupleKeyWithoutCondition {
                            User = userTupleStr,
                            Relation = attr.Relation,
                            Object = prevIdTupleStr,
                        }
                    );

                    // Write current value
                    writeRelations.Add(
                        new ClientTupleKey {
                            User = userTupleStr,
                            Relation = attr.Relation,
                            Object = curIdTupleStr,
                        }
                    );
                }
            }
        }

        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result) {
        // TODO: Implement!

        return base.SavedChanges(eventData, result);
    }
}
