using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OpenFga.Sdk.Client.Model;
using SealedFga.Attributes;
using SealedFga.Database;

namespace SealedFga.Sample.StateSync;

/// <summary>
///     Interceptor for DB save actions.
///     For every changed OpenFGA entity, makes sure the changed relations are sent to the OpenFGA service.
/// </summary>
public class OpenFgaSaveChangesInterceptor : SaveChangesInterceptor {
    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = new()
    ) {
        var context = eventData.Context;
        if (context is null) {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        // Filter for modified entries
        var writeRelations = new List<ClientTupleKey>();
        var deletedRelations = new List<ClientTupleKeyWithoutCondition>();
        var entries = context.ChangeTracker.Entries();
        foreach (var entry in context.ChangeTracker
                                     .Entries()
                                     .Where(e => e.State
                                                     is EntityState.Deleted
                                                        or EntityState.Modified
                                                        or EntityState.Added
                                                 && e.Entity.GetType().GetInterfaces().Any(i =>
                                                     i.IsGenericType && i.GetGenericTypeDefinition() ==
                                                     typeof(IOpenFgaType<>)
                                                 )
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
                var propertyIdTypeAsOpenFgaIdTupleStringMethod = property
                                                                .PropertyType
                                                                .GetMethod(nameof(IOpenFgaTypeId<>
                                                                    .AsOpenFgaIdTupleString)
                                                                 );
                var propertyOpenFgaTupleString = (string) propertyIdTypeAsOpenFgaIdTupleStringMethod
                   .Invoke(entryProperty.CurrentValue, null)!;

                // Retrieve this entity's OpenFGA ID
                var entityIdTypeAsOpenFgaIdTupleStringMethod = entityIdProperty
                                                              .CurrentValue!
                                                              .GetType()
                                                              .GetMethod(nameof(IOpenFgaTypeId<>.AsOpenFgaIdTupleString)
                                                               )!;
                var entityOpenFgaTupleString = (string) entityIdTypeAsOpenFgaIdTupleStringMethod
                   .Invoke(entityIdProperty.CurrentValue, null)!;

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
                    // Only do anything if the ID has actually changed
                    if (entityIdProperty.CurrentValue == entityIdProperty.OriginalValue
                        && entryProperty.CurrentValue == entryProperty.OriginalValue) {
                        continue;
                    }

                    // Retrieve previous values and correctly set them as user/object
                    var prevPropertyIdTupleStr = (string) propertyIdTypeAsOpenFgaIdTupleStringMethod
                                                .Invoke(entryProperty.OriginalValue, null)!;
                    var prevIdTupleStr = (string) entityIdTypeAsOpenFgaIdTupleStringMethod
                        .Invoke(entityIdProperty.OriginalValue, null)!;
                    var (prevUserTupleStr, prevObjTupleStr) = attr.TargetType switch {
                        OpenFgaRelationTargetType.Object => (prevPropertyIdTupleStr, prevIdTupleStr),
                        OpenFgaRelationTargetType.User => (prevIdTupleStr, prevPropertyIdTupleStr),
                    };

                    // Remove previous value
                    deletedRelations.Add(
                        new ClientTupleKeyWithoutCondition {
                            User = prevUserTupleStr,
                            Relation = attr.Relation,
                            Object = prevObjTupleStr,
                        }
                    );

                    // Write current value
                    writeRelations.Add(
                        new ClientTupleKey {
                            User = userTupleStr,
                            Relation = attr.Relation,
                            Object = objTupleStr,
                        }
                    );
                }
            }
        }

        // Writes the FGA relations to the database
        SealedFgaDb.Instance.AddFgaOperations([
                ..writeRelations.Select(wr => new FgaOperation(
                        FgaOperationType.Write,
                        wr.User,
                        wr.Relation,
                        wr.Object
                    )
                ),
                ..deletedRelations.Select(dr => new FgaOperation(
                        FgaOperationType.Delete,
                        dr.User,
                        dr.Relation,
                        dr.Object
                    )
                ),
            ]
        );

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result) {
        // TODO: Implement!

        return base.SavedChanges(eventData, result);
    }
}
