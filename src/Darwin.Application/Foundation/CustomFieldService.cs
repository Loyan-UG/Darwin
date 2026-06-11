using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Foundation;

public sealed class CustomFieldService
{
    private readonly IAppDbContext _db;

    public CustomFieldService(IAppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<Result<Guid>> CreateDefinitionAsync(CreateCustomFieldDefinitionCommand command, CancellationToken ct = default)
    {
        var targetEntityType = FoundationInputNormalizer.Required(command.TargetEntityType);
        var key = FoundationInputNormalizer.Key(command.Key);
        var label = FoundationInputNormalizer.Required(command.Label);
        if (targetEntityType is null)
        {
            return Result<Guid>.Fail("Target entity type is required.");
        }

        if (key is null)
        {
            return Result<Guid>.Fail("Custom field key is required.");
        }

        if (label is null)
        {
            return Result<Guid>.Fail("Custom field label is required.");
        }

        if (FoundationInputNormalizer.LooksSensitive(key) || FoundationInputNormalizer.LooksSensitive(label))
        {
            return Result<Guid>.Fail("Sensitive secrets must not be stored as custom fields.");
        }

        var duplicate = await _db.Set<CustomFieldDefinition>()
            .AnyAsync(x =>
                x.BusinessId == command.BusinessId &&
                x.TargetEntityType == targetEntityType &&
                x.Key == key &&
                !x.IsDeleted,
                ct)
            .ConfigureAwait(false);
        if (duplicate)
        {
            return Result<Guid>.Fail("Custom field definition already exists.");
        }

        var definition = new CustomFieldDefinition
        {
            BusinessId = command.BusinessId,
            TargetEntityType = targetEntityType,
            Key = key,
            Label = label,
            DataType = command.DataType,
            IsRequired = command.IsRequired,
            IsActive = command.IsActive,
            Visibility = command.Visibility,
            ValidationJson = FoundationInputNormalizer.Json(command.ValidationJson),
            MetadataJson = FoundationInputNormalizer.Json(command.MetadataJson)
        };

        _db.Set<CustomFieldDefinition>().Add(definition);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<Guid>.Ok(definition.Id);
    }

    public async Task<Result> UpdateDefinitionAsync(UpdateCustomFieldDefinitionCommand command, CancellationToken ct = default)
    {
        if (command.Id == Guid.Empty)
        {
            return Result.Fail("Custom field definition id is required.");
        }

        var definition = await _db.Set<CustomFieldDefinition>()
            .FirstOrDefaultAsync(x => x.Id == command.Id && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (definition is null)
        {
            return Result.Fail("Custom field definition was not found.");
        }

        var label = FoundationInputNormalizer.Required(command.Label);
        if (label is null)
        {
            return Result.Fail("Custom field label is required.");
        }

        if (FoundationInputNormalizer.LooksSensitive(label))
        {
            return Result.Fail("Sensitive secrets must not be stored as custom fields.");
        }

        definition.Label = label;
        definition.IsRequired = command.IsRequired;
        definition.IsActive = command.IsActive;
        definition.Visibility = command.Visibility;
        definition.ValidationJson = FoundationInputNormalizer.Json(command.ValidationJson);
        definition.MetadataJson = FoundationInputNormalizer.Json(command.MetadataJson);

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Ok();
    }

    public async Task<Result<Guid>> UpsertValueAsync(UpsertCustomFieldValueCommand command, CancellationToken ct = default)
    {
        if (command.DefinitionId == Guid.Empty)
        {
            return Result<Guid>.Fail("Custom field definition id is required.");
        }

        if (command.EntityId == Guid.Empty)
        {
            return Result<Guid>.Fail("Entity id is required.");
        }

        var entityType = FoundationInputNormalizer.Required(command.EntityType);
        if (entityType is null)
        {
            return Result<Guid>.Fail("Entity type is required.");
        }

        var definition = await _db.Set<CustomFieldDefinition>()
            .FirstOrDefaultAsync(x => x.Id == command.DefinitionId && x.IsActive && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (definition is null)
        {
            return Result<Guid>.Fail("Active custom field definition was not found.");
        }

        if (!string.Equals(definition.TargetEntityType, entityType, StringComparison.Ordinal))
        {
            return Result<Guid>.Fail("Custom field definition target does not match entity type.");
        }

        var valueState = BuildTypedValue(definition, command);
        if (!valueState.Succeeded)
        {
            return Result<Guid>.Fail(valueState.Error!);
        }

        var value = await _db.Set<CustomFieldValue>()
            .FirstOrDefaultAsync(x =>
                x.DefinitionId == command.DefinitionId &&
                x.EntityType == entityType &&
                x.EntityId == command.EntityId &&
                !x.IsDeleted,
                ct)
            .ConfigureAwait(false);

        if (value is null)
        {
            value = new CustomFieldValue
            {
                DefinitionId = command.DefinitionId,
                EntityType = entityType,
                EntityId = command.EntityId
            };
            _db.Set<CustomFieldValue>().Add(value);
        }

        value.StringValue = valueState.Value!.StringValue;
        value.NumberValue = valueState.Value.NumberValue;
        value.BooleanValue = valueState.Value.BooleanValue;
        value.DateValue = valueState.Value.DateValue;
        value.JsonValue = valueState.Value.JsonValue;
        value.MetadataJson = FoundationInputNormalizer.Json(command.MetadataJson);

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<Guid>.Ok(value.Id);
    }

    public async Task<IReadOnlyList<CustomFieldValueDto>> GetValuesForEntityAsync(
        string entityType,
        Guid entityId,
        CancellationToken ct = default)
    {
        var normalizedEntityType = FoundationInputNormalizer.Required(entityType);
        if (normalizedEntityType is null || entityId == Guid.Empty)
        {
            return Array.Empty<CustomFieldValueDto>();
        }

        return await (
            from value in _db.Set<CustomFieldValue>().AsNoTracking()
            join definition in _db.Set<CustomFieldDefinition>().AsNoTracking()
                on value.DefinitionId equals definition.Id
            where value.EntityType == normalizedEntityType &&
                  value.EntityId == entityId &&
                  !value.IsDeleted &&
                  definition.IsActive &&
                  !definition.IsDeleted
            orderby definition.Key
            select new CustomFieldValueDto(
                definition.Id,
                value.Id,
                definition.TargetEntityType,
                definition.Key,
                definition.Label,
                definition.DataType,
                definition.Visibility,
                value.StringValue,
                value.NumberValue,
                value.BooleanValue,
                value.DateValue,
                value.JsonValue,
                value.MetadataJson))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private static Result<TypedCustomFieldValue> BuildTypedValue(
        CustomFieldDefinition definition,
        UpsertCustomFieldValueCommand command)
    {
        var text = FoundationInputNormalizer.Optional(command.StringValue);
        var json = FoundationInputNormalizer.Optional(command.JsonValue);
        var hasValue = definition.DataType switch
        {
            CustomFieldDataType.Text => text is not null,
            CustomFieldDataType.Number => command.NumberValue.HasValue,
            CustomFieldDataType.Boolean => command.BooleanValue.HasValue,
            CustomFieldDataType.Date => command.DateValue.HasValue,
            CustomFieldDataType.Json => json is not null,
            _ => false
        };

        if (definition.IsRequired && !hasValue)
        {
            return Result<TypedCustomFieldValue>.Fail("Custom field value is required.");
        }

        if (definition.DataType == CustomFieldDataType.Text &&
            FoundationInputNormalizer.LooksSensitive(text))
        {
            return Result<TypedCustomFieldValue>.Fail("Sensitive secrets must not be stored as custom field values.");
        }

        return Result<TypedCustomFieldValue>.Ok(definition.DataType switch
        {
            CustomFieldDataType.Text => new TypedCustomFieldValue(StringValue: text),
            CustomFieldDataType.Number => new TypedCustomFieldValue(NumberValue: command.NumberValue),
            CustomFieldDataType.Boolean => new TypedCustomFieldValue(BooleanValue: command.BooleanValue),
            CustomFieldDataType.Date => new TypedCustomFieldValue(DateValue: command.DateValue),
            CustomFieldDataType.Json => new TypedCustomFieldValue(JsonValue: json),
            _ => new TypedCustomFieldValue()
        });
    }
}

public sealed record CreateCustomFieldDefinitionCommand(
    Guid? BusinessId,
    string? TargetEntityType,
    string? Key,
    string? Label,
    CustomFieldDataType DataType,
    bool IsRequired = false,
    bool IsActive = true,
    FoundationVisibility Visibility = FoundationVisibility.Internal,
    string? ValidationJson = null,
    string? MetadataJson = null);

public sealed record UpdateCustomFieldDefinitionCommand(
    Guid Id,
    string? Label,
    bool IsRequired,
    bool IsActive,
    FoundationVisibility Visibility,
    string? ValidationJson = null,
    string? MetadataJson = null);

public sealed record UpsertCustomFieldValueCommand(
    Guid DefinitionId,
    string? EntityType,
    Guid EntityId,
    string? StringValue = null,
    decimal? NumberValue = null,
    bool? BooleanValue = null,
    DateTime? DateValue = null,
    string? JsonValue = null,
    string? MetadataJson = null);

public sealed record CustomFieldValueDto(
    Guid DefinitionId,
    Guid ValueId,
    string TargetEntityType,
    string Key,
    string Label,
    CustomFieldDataType DataType,
    FoundationVisibility Visibility,
    string? StringValue,
    decimal? NumberValue,
    bool? BooleanValue,
    DateTime? DateValue,
    string? JsonValue,
    string MetadataJson);

internal sealed record TypedCustomFieldValue(
    string? StringValue = null,
    decimal? NumberValue = null,
    bool? BooleanValue = null,
    DateTime? DateValue = null,
    string? JsonValue = null);
