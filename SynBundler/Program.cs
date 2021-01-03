using System;
using System.Linq;
using System.Threading.Tasks;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.FormKeys.SkyrimSE;

namespace SynBundler
{
    class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance.AddRunnabilityCheck(RunabilityCheck).AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch).Run(args, new RunPreferences() {
                ActionsForEmptyArgs = new RunDefaultPatcher() {
                    IdentifyingModKey = "SynBundler.esp",
                    TargetRelease = GameRelease.SkyrimSE
                }
            });
        }
        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
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
        public static async Task RunabilityCheck(IRunnabilityState state) {
            state.LoadOrder.AssertHasMod(ModKey.FromNameAndExtension("Skyrim.esm"));
        }
    }
}