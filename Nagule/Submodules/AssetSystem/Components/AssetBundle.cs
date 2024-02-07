namespace Nagule;

using System.Reactive;
using Sia;

public struct AssetBundle<TAsset> : IComponentBundle
    where TAsset : notnull
{
    public AssetMetadata Metadata;
    public TAsset Asset;
    public AssetState State;
}

public static class AssetBundle
{
    private static readonly ThreadLocal<IEntityCreator> s_stateEntityCreator =
        new(() => new WorldEntityCreators.Bucket(Context<World>.Current!));

    public static AssetBundle<TAsset> Create<TAsset>(
        World world, in TAsset asset, AssetLife life = AssetLife.Automatic, IAssetRecord? record = null)
        where TAsset : struct
        => new() {
            Metadata = new() {
                AssetType = typeof(TAsset),
                AssetLife = life,
                AssetRecord = record
            },
            Asset = asset,
            State = new(DynEntityRef.Create(
                world.CreateInBucketHost(Unit.Default), s_stateEntityCreator.Value!))
        };
}