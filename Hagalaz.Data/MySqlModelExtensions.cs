using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hagalaz.Data;

/// <summary>
/// Public model-builder helpers for Oracle Connector/NET metadata that is not
/// exposed through public entity-builder extension types in the provider.
/// </summary>
public static class MySqlModelExtensions
{
    private const string CharsetAnnotation = "MySQL:Charset";
    private const string CollationAnnotation = "MySQL:Collation";

    public static ModelBuilder HasCharSet(this ModelBuilder builder, string charset)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Model.SetAnnotation(CharsetAnnotation, charset);
        return builder;
    }

    public static EntityTypeBuilder HasCharSet(this EntityTypeBuilder builder, string charset)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Metadata.SetAnnotation(CharsetAnnotation, charset);
        return builder;
    }

    public static EntityTypeBuilder<TEntity> HasCharSet<TEntity>(this EntityTypeBuilder<TEntity> builder, string charset)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Metadata.SetAnnotation(CharsetAnnotation, charset);
        return builder;
    }

    public static EntityTypeBuilder UseCollation(this EntityTypeBuilder builder, string collation)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Metadata.SetAnnotation(CollationAnnotation, collation);
        return builder;
    }

    public static EntityTypeBuilder<TEntity> UseCollation<TEntity>(this EntityTypeBuilder<TEntity> builder, string collation)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Metadata.SetAnnotation(CollationAnnotation, collation);
        return builder;
    }

    public static PropertyBuilder ForMySQLHasCharset(this PropertyBuilder builder, string charset)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Metadata.SetAnnotation(CharsetAnnotation, charset);
        return builder;
    }
}

// Names used by Connector/NET-generated snapshots. They are intentionally kept
// as thin public shims because the provider's corresponding types are internal.
public static class MySQLModelBuilderExtensions
{
    public static ModelBuilder HasCharSet(ModelBuilder builder, string charset) =>
        MySqlModelExtensions.HasCharSet(builder, charset);
}

public static class MySQLEntityTypeBuilderExtensions
{
    public static EntityTypeBuilder HasCharSet(EntityTypeBuilder builder, string charset) =>
        MySqlModelExtensions.HasCharSet(builder, charset);
}
