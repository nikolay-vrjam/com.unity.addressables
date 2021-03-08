# Unity Addressable Asset system

This repository contains a modified version of the [Unity Addressable Asset system](https://docs.unity3d.com/Packages/com.unity.addressables@1.16/manual/index.html), published under the Unity Companion License (see `LICENSE.md` for details).

The modifications are currently based on Addressables version [1.16.15](https://docs.unity3d.com/Packages/com.unity.addressables@1.16/changelog/CHANGELOG.html#11615---2020-12-09).

## Building Multiple Catalogs in Packed Mode

This repository extends the [`BuildScriptPackedMode`](https://docs.unity3d.com/Packages/com.unity.addressables@1.16/api/UnityEditor.AddressableAssets.Build.DataBuilders.BuildScriptPackedMode.html) script in a way that it can build multiple catalogs. The intended way to use this feature is to implement a new class inheriting from it that overrides the new function `GetContentCatalogs`, which defaults to the originally single main catalog written into `catalog.json`.

It provides a new build script `BuildScriptPackedMultipleCatalogs` for demonstration purposes that may already be sufficient for your needs. If not, use that script as a base to create one that suits your needs.

### Usage

1. Create a new asset from *Create &rarr; Addressables &rarr; Content Builders &rarr; Multiple Catalogs* in the project browser context menu. By default, it is named `BuildScriptPackedMultipleCatalogs` like the class.
2. Edit the asset setting it up as desired.
   * *Catalogs* is the list of catalogs to build *in addition to* the default catalog (`catalog.json`). Any entry here will create an additional catalog containing all assets whose addresses begin with the catalog name. For example, if you add a catalog named `example`, then it will be built in a file `example.json` and contain all assets whose addresses start with `example`. These assets will no longer be contained in the default catalog and not be loaded automatically on application startup. See [Runtime Loading](#runtime-loading) for details.
   * *Catalog Build Path* is the local project path where the catalogs will be built to.
   * *Runtime Load Path* tells where, in your built project, the catalog files will be contained. See [Runtime Loading](#runtime-loading) for additional notes.
3. Head to *Window &rarr; Asset Management &rarr; Addressables &rarr; Settings* from the main menu and assign the new asset to the *Default Build Script* field.
4. Build your asset bundles via *Build &rarr; Default Build Script* from the *Addressables Groups* window and find the additional catalogs in the configured path.

In case you wish to revert to the original default build script, it is located under `Assets/AddressableAssetsData/DataBuilders/BuildScriptPackedMode` in your project.

### Limitations

* The *BuildRemoteCatalog* settings does not support building multiple catalogs yet.
* Dependencies of extra catalogs are *not* loaded automatically. You must make sure extra catalogs are dynamically loaded in correct order.
  * During build, a warning will be emitted for every encountered cross-catalog dependency.

### Runtime Loading

Additional catalogs can be loaded as follows:

```c#
var asyncHandle = Addressables.LoadContentCatalogAsync(catalogPath, true);
```

The `catalogPath` must point to the JSON file of the catalog to be loaded.

The Addressables build system hardcodes the runtime path of catalog files into the JSON, which can be quite a nuisance. For example, this is bad in case you would like to support mod folders with arbitrary directory structures. Luckily, there is a simple trick to allow catalogs to be installed in any directory by using properties.

Set your `RuntimeLoadPathPrefix` to `{Current}/` (include the slash!). Now, alter the loading code as follows:

```c#
AddressablesRuntimeProperties.SetPropertyValue("Current", Path.GetDirectoryName(catalogPath));
var asyncHhandle = Addressables.LoadContentCatalogAsync(catalogPath, true);
```

This sets the property `Current` to the path of the JSON file, such that the `.bundle` files will now conveniently be loaded from that same directory.