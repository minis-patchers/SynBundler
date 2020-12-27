using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using SynBundler.Types;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.FormKeys.SkyrimSE;

namespace SynBundler
{
    class Program
    {
        public static int Main(string[] args)
        {
            return SynthesisPipeline.Instance.Patch<ISkyrimMod, ISkyrimModGetter>(
                args: args,
                patcher: RunPatch,
                new UserPreferences() {
                ActionsForEmptyArgs = new RunDefaultPatcher
                {
                    IdentifyingModKey = "SynBundler.esp",
                    TargetRelease = GameRelease.SkyrimSE
                }
            });
        }
        public static void RunPatch(SynthesisState<ISkyrimMod, ISkyrimModGetter> state)
        {
            var Config = JObject.Parse(Path.Combine(state.ExtraSettingsDataPath, "config.json")).ToObject<BundlerConfig>();
            if(Config.AllowExploits) {
                Console.WriteLine("Allowing exploits allows an infinite EXP farm, temptation is killer, you have been warned");
            }
            foreach(var abt in state.LoadOrder.PriorityOrder.Ammunition().WinningOverrides()) {
                if(abt.Keywords?.Contains(Skyrim.Keyword.VendorItemArrow)??false && !String.IsNullOrEmpty(abt.Name.String)) {
                    var miscitem = state.PatchMod.MiscItems.AddNew($"bundled_{abt.EditorID}");
                    miscitem.Model = abt.Model?.DeepCopy();
                    miscitem.Keywords = new Noggog.ExtendedList<IFormLink<IKeywordGetter>>();
                    miscitem.Keywords?.Add(Skyrim.Keyword.VendorItemArrow);
                    miscitem.Name = $"Bundle of {abt.Name}";
                    miscitem.Value = 10 * abt.Value;
                    Console.WriteLine($"Generating {miscitem.Name}");
                    var bundler = state.PatchMod.ConstructibleObjects.AddNew($"bundle_{abt.EditorID}");
                    bundler.CreatedObject = miscitem.FormKey;
                    bundler.CreatedObjectCount = 1;
                    bundler.Items = new Noggog.ExtendedList<ContainerEntry>();
                    bundler.Items.Add(new ContainerEntry(){
                        Item = new ContainerItem() {
                            Item = abt.FormKey,
                            Count =  10
                        }
                    });
                    bundler.WorkbenchKeyword = Skyrim.Keyword.CraftingTanningRack;
                    bundler.Conditions.Add(new ConditionFloat() {
                        CompareOperator = CompareOperator.GreaterThanOrEqualTo,
                        ComparisonValue = 10,
                        Data = new FunctionConditionData() {
                            Function = (ushort)ConditionData.Function.GetItemCount,
                            ParameterOneRecord = abt.FormKey
                        }
                    });
                    if(Config.AllowExploits) {
                        var unbundler = state.PatchMod.ConstructibleObjects.AddNew($"unbundle_{abt.EditorID}");
                        unbundler.CreatedObject = abt.FormKey;
                        unbundler.CreatedObjectCount = 10;
                        unbundler.Items = new Noggog.ExtendedList<ContainerEntry>();
                        unbundler.Items.Add(new ContainerEntry(){
                            Item = new ContainerItem() {
                                Item = miscitem.FormKey,
                                Count =  1
                            }
                        });
                        unbundler.WorkbenchKeyword = Skyrim.Keyword.CraftingTanningRack;
                        unbundler.Conditions.Add(new ConditionFloat() {
                            CompareOperator = CompareOperator.GreaterThanOrEqualTo,
                            ComparisonValue = 1,
                            Data = new FunctionConditionData() {
                                Function = (ushort)ConditionData.Function.GetItemCount,
                                ParameterOneRecord = miscitem.FormKey
                            }
                        });
                    }
                }
            }
        }
    }
}