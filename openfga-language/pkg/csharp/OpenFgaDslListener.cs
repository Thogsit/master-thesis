using System.Collections.Generic;
using OpenFga.Language.Model;
using OpenFga.Language.Util;

namespace OpenFga.Language;

public class OpenFgaDslListener(OpenFGAParser parser) : OpenFGAParserBaseListener
{
    private const string RelationDefinitionOperatorOr = "or";
    private const string RelationDefinitionOperatorAnd = "and";
    private const string RelationDefinitionOperatorButNot = "but not";

    private readonly AuthorizationModel _authorizationModel = new();
    private TypeDefinition? _currentTypeDef;
    private Relation? _currentRelation;
    private Condition? _currentCondition;
    private bool _isModularModel;
    private readonly Dictionary<string, TypeDefinition> _typeDefExtensions = new();

    private Stack<StackRelation>? _rewriteStack;

    public AuthorizationModel GetAuthorizationModel()
    {
        return _authorizationModel;
    }

    private Userset? ParseExpression(List<Userset> rewrites, string operatorType)
    {
        return rewrites.Count switch
        {
            0 => null,
            1 => rewrites[0],
            _ => operatorType switch
            {
                RelationDefinitionOperatorOr => new Userset(union: new Usersets(child: rewrites)),
                RelationDefinitionOperatorAnd => new Userset(intersection: new Usersets(child: rewrites)),
                RelationDefinitionOperatorButNot => new Userset(difference: new Difference(_base: rewrites[0],
                    subtract: rewrites[1])),
                _ => null
            }
        };
    }

    public override void EnterMain(OpenFGAParser.MainContext context)
    {
        _authorizationModel.Conditions = new Dictionary<string, Condition>();
    }

    public override void ExitModelHeader(OpenFGAParser.ModelHeaderContext context)
    {
        if (context.SCHEMA_VERSION() != null)
        {
            _authorizationModel.SchemaVersion = context.SCHEMA_VERSION().GetText();
        }
    }

    public override void ExitModuleHeader(OpenFGAParser.ModuleHeaderContext context)
    {
        _isModularModel = true;
    }

    public override void EnterTypeDefs(OpenFGAParser.TypeDefsContext context)
    {
        _authorizationModel.TypeDefinitions = [];
    }

    public override void EnterTypeDef(OpenFGAParser.TypeDefContext context)
    {
        if (context.typeName == null)
        {
            return;
        }

        if (context.EXTEND() != null && !_isModularModel)
        {
            parser.NotifyErrorListeners(context.typeName.Start, "extend can only be used in a modular model", null);
        }

        _currentTypeDef = new TypeDefinition
        {
            Type = context.typeName.GetText(),
            Relations = new Dictionary<string, Userset>(),
            Metadata = new Metadata { Relations = new Dictionary<string, RelationMetadata>() }
        };
    }

    public override void EnterConditions(OpenFGAParser.ConditionsContext context)
    {
        _authorizationModel.Conditions = new Dictionary<string, Condition>();
    }

    public override void EnterCondition(OpenFGAParser.ConditionContext context)
    {
        if (context.conditionName() == null)
        {
            return;
        }

        var conditionName = context.conditionName().GetText();
        if (_authorizationModel.Conditions != null && _authorizationModel.Conditions.ContainsKey(conditionName))
        {
            var message = $"condition '{conditionName}' is already defined in the model";
            parser.NotifyErrorListeners(context.conditionName().Start, message, null);
        }

        _currentCondition = new Condition
        {
            Name = conditionName,
            Expression = "",
            Parameters = new Dictionary<string, ConditionParamTypeRef>()
        };
    }

    public override void ExitConditionParameter(OpenFGAParser.ConditionParameterContext context)
    {
        if (context.parameterName() == null || context.parameterType() == null)
        {
            return;
        }

        var parameterName = context.parameterName().GetText();
        if (_currentCondition?.Parameters != null && _currentCondition.Parameters.ContainsKey(parameterName))
        {
            var message = $"parameter '{parameterName}' is already defined in the condition '{_currentCondition.Name}'";
            parser.NotifyErrorListeners(context.parameterName().Start, message, null);
        }

        var paramContainer = context.parameterType().CONDITION_PARAM_CONTAINER();
        var conditionParamTypeRef = new PartialConditionParamTypeRef();
        var typeName = context.parameterType().GetText();
        if (paramContainer != null)
        {
            typeName = paramContainer.GetText();
            conditionParamTypeRef.TypeName = ParseTypeName(paramContainer.GetText());
            if (context.parameterType().CONDITION_PARAM_TYPE() != null)
            {
                var genericTypeName = ParseTypeName(context.parameterType().CONDITION_PARAM_TYPE().GetText());
                conditionParamTypeRef.GenericTypes = [new ConditionParamTypeRef { TypeName = genericTypeName }];
            }
        }

        conditionParamTypeRef.TypeName = ParseTypeName(typeName);

        if (_currentCondition is not null)
        {
            _currentCondition.Parameters![parameterName] = conditionParamTypeRef.AsConditionParamTypeRef();
        }
    }

    private TypeName ParseTypeName(string typeName)
    {
        return EnumUtil.FromString<TypeName>($"TYPE_NAME_{typeName.ToUpper()}");
    }

    public override void ExitConditionExpression(OpenFGAParser.ConditionExpressionContext context)
    {
        _currentCondition!.Expression = context.GetText().Trim();
    }

    public override void ExitCondition(OpenFGAParser.ConditionContext context)
    {
        if (_currentCondition is null)
        {
            return;
        }

        _authorizationModel.Conditions![_currentCondition.Name] = _currentCondition;
        _currentCondition = null;
    }

    public override void ExitTypeDef(OpenFGAParser.TypeDefContext context)
    {
        if (_currentTypeDef == null)
        {
            return;
        }

        if (_currentTypeDef.Metadata?.Relations is { Count: 0 })
        {
            _currentTypeDef.Metadata = null;
        }

        _authorizationModel.TypeDefinitions.Add(_currentTypeDef);

        if (context.EXTEND() != null && _isModularModel)
        {
            if (_typeDefExtensions.ContainsKey(_currentTypeDef.Type))
            {
                parser.NotifyErrorListeners(
                    context.typeName.Start,
                    $"'{_currentTypeDef.Type}' is already extended in file.",
                    null);
            }
            else
            {
                _typeDefExtensions[_currentTypeDef.Type] = _currentTypeDef;
            }
        }

        _currentTypeDef = null;
    }

    public override void EnterRelationDeclaration(OpenFGAParser.RelationDeclarationContext context)
    {
        _currentRelation = new Relation(
            null,
            [],
            null,
            new RelationMetadata { DirectlyRelatedUserTypes = [] }
        );
        _rewriteStack = new Stack<StackRelation>();
    }

    public override void ExitRelationDeclaration(OpenFGAParser.RelationDeclarationContext context)
    {
        if (context.relationName() == null)
        {
            return;
        }

        var relationName = context.relationName().GetText();

        var relationDef = ParseExpression(_currentRelation!.Rewrites, _currentRelation!.Operator!);
        if (relationDef != null)
        {
            if (_currentTypeDef!.Relations!.ContainsKey(relationName))
            {
                var message = $"'{relationName}' is already defined in '{_currentTypeDef.Type}'";
                parser.NotifyErrorListeners(context.relationName().Start, message, null);
            }

            _currentTypeDef!.Relations![relationName] = relationDef;
            var directlyRelatedUserTypes = _currentRelation.TypeInfo.DirectlyRelatedUserTypes;
            _currentTypeDef!.Metadata!.Relations![relationName] = new RelationMetadata
            {
                DirectlyRelatedUserTypes = directlyRelatedUserTypes
            };
        }

        _currentRelation = null;
    }

    public override void EnterRelationDefDirectAssignment(OpenFGAParser.RelationDefDirectAssignmentContext context)
    {
        _currentRelation!.TypeInfo = new RelationMetadata { DirectlyRelatedUserTypes = [] };
    }

    public override void ExitRelationDefDirectAssignment(OpenFGAParser.RelationDefDirectAssignmentContext context)
    {
        var partialRewrite = new Userset(_this: new Dictionary<string, object>());
        _currentRelation!.Rewrites.Add(partialRewrite);
    }

    public override void ExitRelationDefTypeRestriction(OpenFGAParser.RelationDefTypeRestrictionContext context)
    {
        var baseRestriction = context.relationDefTypeRestrictionBase();
        if (baseRestriction == null)
        {
            return;
        }

        var type = baseRestriction.relationDefTypeRestrictionType;
        var usersetRestriction = baseRestriction.relationDefTypeRestrictionRelation;
        var wildcardRestriction = baseRestriction.relationDefTypeRestrictionWildcard;
        var conditionName = context.conditionName();

        var relationRef = new PartialRelationReference(
            type?.GetText(),
            usersetRestriction?.GetText(),
            wildcardRestriction is null ? new Dictionary<string, object>() : null,
            conditionName?.GetText()
        );

        _currentRelation!.TypeInfo.DirectlyRelatedUserTypes!.Add(relationRef.AsRelationReference());
    }

    public override void ExitRelationDefRewrite(OpenFGAParser.RelationDefRewriteContext context)
    {
        var computedUserset = new ObjectRelation(relation: context.rewriteComputedusersetName.GetText());

        var partialRewrite = context.rewriteTuplesetName == null
            ? new Userset(computedUserset: computedUserset)
            : new Userset(tupleToUserset: new TupleToUserset
            {
                ComputedUserset = computedUserset,
                Tupleset = new ObjectRelation(relation: context.rewriteTuplesetName.GetText())
            });

        _currentRelation!.Rewrites.Add(partialRewrite);
    }

    public override void ExitRelationRecurse(OpenFGAParser.RelationRecurseContext context)
    {
        if (_currentRelation == null)
        {
            return;
        }

        var relationDef = ParseExpression(_currentRelation.Rewrites, _currentRelation.Operator!);

        if (relationDef != null)
        {
            _currentRelation.Rewrites = [relationDef];
        }
    }

    public override void EnterRelationRecurseNoDirect(OpenFGAParser.RelationRecurseNoDirectContext context)
    {
        _rewriteStack?
            .Push(new StackRelation(_currentRelation!.Rewrites, _currentRelation!.Operator!));
        _currentRelation!.Rewrites = [];
    }

    public override void ExitRelationRecurseNoDirect(OpenFGAParser.RelationRecurseNoDirectContext context)
    {
        if (_currentRelation == null)
        {
            return;
        }

        var popped = _rewriteStack!.Pop();

        var relationDef = ParseExpression(_currentRelation.Rewrites, _currentRelation.Operator!);
        if (relationDef != null)
        {
            _currentRelation.Operator = popped.Operator;
            _currentRelation.Rewrites = [..popped.Rewrites, relationDef];
        }
    }

    public override void EnterRelationDefPartials(OpenFGAParser.RelationDefPartialsContext context)
    {
        if (context.OR().Length > 0)
        {
            _currentRelation!.Operator = RelationDefinitionOperatorOr;
        }
        else if (context.AND().Length > 0)
        {
            _currentRelation!.Operator = RelationDefinitionOperatorAnd;
        }
        else if (context.BUT_NOT() != null)
        {
            _currentRelation!.Operator = RelationDefinitionOperatorButNot;
        }
    }
}