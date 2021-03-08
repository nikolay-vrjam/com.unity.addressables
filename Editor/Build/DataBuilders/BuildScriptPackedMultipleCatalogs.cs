using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace UnityEditor.AddressableAssets.Build.DataBuilders
{
    /// <summary>
    /// Build script used for player builds and running with bundles in the editor, allowing building of multiple catalogs.
    /// </summary>
    [CreateAssetMenu(fileName = "BuildScriptPackedMultipleCatalogs.asset", menuName = "Addressables/Content Builders/Multiple Catalogs")]
    public class BuildScriptPackedMultipleCatalogs : BuildScriptPackedMode
    {
        /// <summary>
        /// Move a file, deleting it first if it exists.
        /// </summary>
        /// <param name="src">the file to move</param>
        /// <param name="dst">the destination</param>
        private static void FileMoveOverwrite(string src, string dst)
        {
            if (File.Exists(dst))
            {
                File.Delete(dst);
            }
            File.Move(src, dst);
        }

        [Tooltip("The catalogs to be built in addition to the main catalog. The catalog names listed here will contain all assets whose addresses begin with their name.")]
        public string[] Catalogs;

        [Tooltip("The local directory into which to move the catalogs.")]
        public string CatalogsBuildPath = "Library/Catalogs";

        [Tooltip("The runtime load path.")]
        public string RuntimeLoadPath = "Catalogs/";

        private class CatalogSetup
        {
            /// <summary>
            /// The address prefix of this catalog.
            /// </summary>
            public readonly string AddressPrefix;

            /// <summary>
            /// The catalog build info.
            /// </summary>
            public readonly ContentCatalogBuildInfo BuildInfo;

            /// <summary>
            /// The files associated to the catalog.
            /// </summary>
            public readonly List<string> Files = new List<string>(1);

            /// <summary>
            /// Tells whether the catalog is empty.
            /// </summary>
            public bool Empty => BuildInfo.Locations.Count == 0;

            public CatalogSetup(string addressPrefix)
            {
                AddressPrefix = addressPrefix;
                BuildInfo = new ContentCatalogBuildInfo(addressPrefix, addressPrefix + ".json");
                BuildInfo.Register = false;
            }
        }

        private readonly List<CatalogSetup> _catalogSetups = new List<CatalogSetup>();

        protected override List<ContentCatalogBuildInfo> GetContentCatalogs(AddressablesDataBuilderInput builderInput, AddressableAssetsBuildContext aaContext)
        {
            // cleanup
            _catalogSetups.Clear();

            // prepare catalogs
            var defaultCatalog = new ContentCatalogBuildInfo(ResourceManagerRuntimeData.kCatalogAddress, builderInput.RuntimeCatalogFilename);
            foreach (var prefix in Catalogs)
            {
                _catalogSetups.Add(new CatalogSetup(prefix));
            }

            // assign assets to catalogs based on their addresses
            foreach (var loc in aaContext.locations)
            {
                var excludeFromDefault = false;
                var address = loc.Keys.Count > 0 ? loc.Keys[0].ToString() : "";
                if (address.Length > 0)
                {
                    foreach (var setup in _catalogSetups)
                    {
                        if (address.StartsWith(setup.AddressPrefix))
                        {
                            if (loc.ResourceType == typeof(IAssetBundleResource))
                            {
                                var filePath = Path.GetFullPath(loc.InternalId.Replace("{UnityEngine.AddressableAssets.Addressables.RuntimePath}", Addressables.BuildPath));
                                setup.Files.Add(filePath);

                                var fileName = Path.GetFileName(filePath);

                                var localLoadPath = RuntimeLoadPath + fileName;
                                setup.BuildInfo.Locations.Add(new ContentCatalogDataEntry(typeof(IAssetBundleResource), localLoadPath, loc.Provider, loc.Keys, loc.Dependencies, loc.Data));
                            }
                            else
                            {
                                setup.BuildInfo.Locations.Add(loc);
                            }

                            excludeFromDefault = true;
                            break;
                        }
                    }
                }

                if (!excludeFromDefault)
                {
                    defaultCatalog.Locations.Add(loc);
                }
            }

            // gather catalogs
            var catalogs = new List<ContentCatalogBuildInfo>(_catalogSetups.Count + 1);
            Debug.Log("adding default catalog containing " + defaultCatalog.Locations.Count + " assets to build");
            catalogs.Add(defaultCatalog);
            foreach (var setup in _catalogSetups)
            {
                if (!setup.Empty)
                {
                    Debug.Log("adding catalog " + setup.AddressPrefix + " containing " + setup.BuildInfo.Locations.Count + " assets to build");
                    catalogs.Add(setup.BuildInfo);
                }
            }
            return catalogs;
        }

        protected override TResult DoBuild<TResult>(AddressablesDataBuilderInput builderInput, AddressableAssetsBuildContext aaContext)
        {
            // execute build script
            var result = base.DoBuild<TResult>(builderInput, aaContext);

            // move extra catalogs to CatalogsBuildPath
            foreach (var setup in _catalogSetups)
            {
                var bundlePath = Path.Combine(CatalogsBuildPath, setup.AddressPrefix);

                Directory.CreateDirectory(bundlePath);

                FileMoveOverwrite(Path.Combine(Addressables.BuildPath, setup.BuildInfo.JsonFilename), Path.Combine(bundlePath, setup.BuildInfo.JsonFilename));
                foreach (var file in setup.Files)
                {
                    FileMoveOverwrite(file, Path.Combine(bundlePath, Path.GetFileName(file)));
                }
            }

            return result;
        }
    }
}
