using System;
using System.Linq;
using System.Threading.Tasks;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Noggog;

namespace SynBundler
{
    internal class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "SynBundler.esp")
                .Run(args);
        }
        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            foreach (var abt in state.LoadOrder.PriorityOrder.Ammunition().WinningOverrides())
            {
                if (abt.Keywords?.Contains(Skyrim.Keyword.VendorItemArrow) ?? (false && !abt.Name.String.IsNullOrEmpty()))
                {
                    var miscitem = state.PatchMod.MiscItems.AddNew($"bundled_{abt.EditorID}");
                    miscitem.Model = abt.Model?.DeepCopy();
                    miscitem.Keywords = new();
                    miscitem.Keywords?.Add(Skyrim.Keyword.VendorItemArrow);
                    miscitem.Name = $"Bundle of {abt.Name}";
                    miscitem.Value = 10 * abt.Value;
                    Console.WriteLine($"Generating {miscitem.Name}");
                    var bundler = state.PatchMod.ConstructibleObjects.AddNew($"bundle_{abt.EditorID}");
                    bundler.CreatedObject.SetTo(miscitem);
                    bundler.CreatedObjectCount = 1;
                    bundler.Items = new ExtendedList<ContainerEntry>
                    {
                        new ContainerEntry()
                        {
                            Item = new ContainerItem()
                            {
                                Item = abt.AsLink(),
                                Count = 10
                            }
                        }
                    };
                    bundler.WorkbenchKeyword.SetTo(Skyrim.Keyword.CraftingTanningRack);
                    bundler.Conditions.Add(new ConditionFloat()
                    {
                        CompareOperator = CompareOperator.GreaterThanOrEqualTo,
                        ComparisonValue = 10,
                        Data = new FunctionConditionData()
                        {
                            Function = ConditionData.Function.GetItemCount,
                            ParameterOneRecord = abt.AsLink()
                        }
                    });
                    var unbundler = state.PatchMod.ConstructibleObjects.AddNew($"unbundle_{abt.EditorID}");
                    unbundler.CreatedObject.SetTo(abt);
                    unbundler.CreatedObjectCount = 10;
                    unbundler.Items = new ExtendedList<ContainerEntry>
                    {
                        new ContainerEntry()
                        {
                            Item = new ContainerItem()
                            {
                                Item = miscitem.AsLink(),
                                Count = 1
                            }
                        }
                    };
                    unbundler.WorkbenchKeyword.SetTo(Skyrim.Keyword.CraftingTanningRack);
                    unbundler.Conditions.Add(new ConditionFloat()
                    {
                        CompareOperator = CompareOperator.GreaterThanOrEqualTo,
                        ComparisonValue = 1,
                        Data = new FunctionConditionData()
                        {
                            Function = ConditionData.Function.GetItemCount,
                            ParameterOneRecord = miscitem.AsLink()
                        }
                    });
                }
            }
        }
    }
}