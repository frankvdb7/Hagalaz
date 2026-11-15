namespace Hagalaz.Cache.Abstractions.Types;

/// <summary>An animation skeleton (cache index 1). Values are as stored, one entry per slot.</summary>
public interface IAnimationSkeletonType : IType
{
    /// <summary>The transform type byte of each slot (the client treats 6 as 2).</summary>
    int[] Types { get; set; }
    /// <summary>The enabled byte of each slot (1 means enabled).</summary>
    int[] Enabled { get; set; }
    int[] Masks { get; set; }
    /// <summary>The model skin groups each slot moves.</summary>
    int[][] Groups { get; set; }
}
