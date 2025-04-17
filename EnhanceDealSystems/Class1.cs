using MelonLoader;
using HarmonyLib;
using Il2CppScheduleOne;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Product;
using UnityEngine;
using Il2CppScheduleOne.Dialogue;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Messaging;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.UI.Handover;
using Il2CppScheduleOne.UI.Phone.Messages;
using Il2CppSystem.Reflection;

[assembly: MelonInfo(typeof(EnhanceDealSystems.EntryPoint), "EnhanceDealSystems", "1.0.6", "_peron")]

namespace EnhanceDealSystems
{
    public class EntryPoint : MelonMod
    {
        public static float EvaluateCounterofferPercentage(ProductDefinition product, int quantity, float price, Customer customer)
        {
            float adjustedWeeklySpend =
                customer.customerData.GetAdjustedWeeklySpend(customer.NPC.RelationData.RelationDelta / 5f);
            Il2CppSystem.Collections.Generic.List<EDay> orderDays =
                customer.customerData.GetOrderDays(customer.CurrentAddiction,
                    customer.NPC.RelationData.RelationDelta / 5f);
            float num = adjustedWeeklySpend / orderDays.Count;

            // Immediate rejection based on price threshold
            if (price >= num * 3f)
                return 0f;

            float valueProposition = Customer.GetValueProposition(
                Registry.GetItem<ProductDefinition>(customer.OfferedContractInfo.Products.entries[0].ProductID),
                customer.OfferedContractInfo.Payment / customer.OfferedContractInfo.Products.entries[0].Quantity
            );
            float productEnjoyment =
                customer.GetProductEnjoyment(product, customer.customerData.Standards.GetCorrespondingQuality());
            float num2 = Mathf.InverseLerp(-1f, 1f, productEnjoyment);
            float valueProposition2 = Customer.GetValueProposition(product, price / quantity);
            float num3 = Mathf.Pow(quantity / (float)customer.OfferedContractInfo.Products.entries[0].Quantity, 0.6f);
            float num4 = Mathf.Lerp(0f, 2f, num3 * 0.5f);
            float num5 = Mathf.Lerp(1f, 0f, Mathf.Abs(num4 - 1f));

            // High value proposition leads to acceptance
            if (valueProposition2 * num5 > valueProposition)
                return 100f;

            // Low value proposition leads to rejection
            if (valueProposition2 < 0.12f)
                return 0f;

            float num6 = productEnjoyment * valueProposition;
            float num7 = num2 * num5 * valueProposition2;

            // Better product enjoyment and proposition leads to acceptance
            if (num7 > num6)
                return 100f;

            float num8 = num6 - num7;
            float num9 = Mathf.Lerp(0f, 1f, num8 / 0.2f);
            float t = Mathf.Max(customer.CurrentAddiction, customer.NPC.RelationData.NormalizedRelationDelta);
            float num10 = Mathf.Lerp(0f, 0.2f, t);

            // Calculate probabilistic acceptance chance
            if (num9 <= num10)
                return 100f;
            if (num9 - num10 >= 0.9f)
                return 0f;

            float probability = (0.9f + num10 - num9) / 0.9f;
            return Mathf.Clamp(probability, 0f, 1f) * 100f;
        }
        
        
        
    }
    
    
    public static class Config
    {
        
        public static bool SuperAutoHandover = true;
        public static bool AutoScheduleDeals = true;
        public static bool AutoCounterOffer = true;
        public static int DealDayTime = 4;
        public static bool AutoHandOver = true;
        public static bool AutoTrack = true;
        public static bool MagicHandover = true;
        public static void OnLoad()
        {
            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string text = Path.Combine(Path.Combine(directoryName,"Mods"), "EnhanceDealSystems");
            bool flag = !Directory.Exists(text);
            if (flag)
            {
                Directory.CreateDirectory(text);
            }
            string path = Path.Combine(text, "config.ini");
            bool flag2 = !File.Exists(path);
            if (flag2)
            {
                string[] contents = new string[]
                {
                    "# EnhanceDealSystems configuration",
                    "AutoScheduleDeals=true",
                    "AutoCounterOffer=true",
                    "# 1= morning 2= evening 3= night 4 late night",
                    "DealDayTime=4",
                    "#Super auto handover will complete the deal when pressing E into a npc waiting for a handover in the correct place, auto hand over will still open the dialog box and you'll need to press one, if you don't want superautohandover set it to false",
                    "AutoHandOver=true",
                    "SuperAutoHandover=true",
                    "#Set it to false to avoid autoamtically tracking deals on the left",
                    "AutoTrack=true",
                    "#This option is a really op option, so it cames off by default, with this on you will complet a deal as soon as you get close to the NPC (<4meters)",
                    "MagicHandover=false"
                };
                File.WriteAllLines(path, contents);
                MelonLogger.Msg("Config file created with default values.");
            }
            bool lastupdated = false;
            string[] array = File.ReadAllLines(path);
            foreach (string text2 in array)
            {
                bool flag3 = string.IsNullOrWhiteSpace(text2) || text2.TrimStart().StartsWith("#");
                if (!flag3)
                {
                    string[] array3 = text2.Split('=', StringSplitOptions.None);
                    bool flag4 = array3.Length < 2;
                    if (!flag4)
                    {
                        string text3 = array3[0].Trim();
                        string text4 = array3[1].Trim();
                        if (text3.Equals("AutoScheduleDeals", StringComparison.OrdinalIgnoreCase))
                        {
                            AutoScheduleDeals = bool.Parse(text4);
                        } else if (text3.Equals("AutoCounterOffer", StringComparison.OrdinalIgnoreCase))
                        {
                            AutoCounterOffer = bool.Parse(text4);
                        }
                        else if (text3.Equals("AutoHandOver", StringComparison.OrdinalIgnoreCase))
                        {
                            AutoHandOver = bool.Parse(text4);
                        }
                        else if (text3.Equals("DealDayTime", StringComparison.OrdinalIgnoreCase))
                        {
                            DealDayTime = int.Parse(text4);
                        }
                        else if (text3.Equals("SuperAutoHandover", StringComparison.OrdinalIgnoreCase))
                        {
                            
                            SuperAutoHandover = bool.Parse(text4);
                        }
                        else if (text3.Equals("AutoTrack",StringComparison.OrdinalIgnoreCase))
                        {
                            AutoTrack = bool.Parse(text4);
                        }
                        else if (text3.Equals("MagicHandover", StringComparison.OrdinalIgnoreCase))
                        {
                            lastupdated = true;
                            MagicHandover = bool.Parse(text4);
                        }
                    }
                }
                
            }

            if (!lastupdated)
            {
                {
                    string[] contents = new string[]
                    {
                        "# EnhanceDealSystems configuration",
                        "AutoScheduleDeals=true",
                        "AutoCounterOffer=true",
                        "# 1= morning 2= evening 3= night 4 late night",
                        "DealDayTime=4",
                        "#Super auto handover will complete the deal when pressing E into a npc waiting for a handover in the correct place, auto hand over will still open the dialog box and you'll need to press one, if you don't want superautohandover set it to false",
                        "AutoHandOver=true",
                        "SuperAutoHandover=true",
                        "#Set it to false to avoid autoamtically tracking deals on the left",
                        "AutoTrack=true",
                        "#This option is a really op option, so it cames off by default, with this on you will complet a deal as soon as you get close to the NPC (<4meters)",
                        "MagicHandover=false"
                    };
                    File.WriteAllLines(path, contents);
                    MelonLogger.Msg("Config file created with default values.");
                }
            }

        }
    }







    /* Too dificult tbh
    [HarmonyPatch(typeof(Il2CppScheduleOne.Levelling.LevelManager))]
    class LevelQuest
    {
        [HarmonyPatch("AddXP")]
        [HarmonyPostfix]
        static void UpdateQuest1(Il2CppScheduleOne.Levelling.LevelManager __instance, int xp)
        {
            //THIS IS FOR XP QUEST CODE
        }

        [HarmonyPatch("AddXPLocal")]
        [HarmonyPostfix]
        static void UpdateQuest2(Il2CppScheduleOne.Levelling.LevelManager __instance, int xp)
        {
            //THIS IS FOR XP QUEST CODE
        }
    }
    */
    
    [HarmonyPatch(typeof(Il2CppScheduleOne.PlayerScripts.Player))]
    [HarmonyPatch("PlayerLoaded")]
    class ConfigLoad
    {
        [HarmonyPostfix]
        static void PostfixPlayerLoaded(Il2CppScheduleOne.PlayerScripts.Player __instance)
        {
            Config.OnLoad();
        }
    }

    class HandleDeal
    {
        public static bool DoDeal(Customer __instance)
        {
            if (!Config.AutoHandOver) return true;
            if (__instance.CurrentContract?.ProductList?.entries == null ||
                __instance.CurrentContract.ProductList.entries.Count == 0)
            {
                return true;
            }

            ProductList.Entry targetProduct =
                __instance.CurrentContract.ProductList.entries[0];
            string targetProductId = targetProduct.ProductID;
            uint totalQuantityNeeded = (uint)targetProduct.Quantity;

            if (totalQuantityNeeded == 0)
            {
                return true;
            }

            /*uint playerTotalQuantity = PlayerSingleton<PlayerInventory>.Instance.GetAmountOfItem(targetProductId);
            if (playerTotalQuantity < totalQuantityNeeded) {
                return true;
            }
            */

            List<ItemSlot> brickSlots = new List<ItemSlot>();
            List<ItemSlot> jarSlots = new List<ItemSlot>();
            List<ItemSlot> unitSlots = new List<ItemSlot>();
            foreach (var itemSlot in Player.Local.Inventory)
            {
                if (itemSlot?.ItemInstance != null &&
                    itemSlot.ItemInstance.ID == targetProductId &&
                    itemSlot.Quantity > 0)
                {
                    ItemInstance itemInstance = itemSlot.ItemInstance;
                    string packagingId = null;
                    string itemTypeName = itemInstance.GetIl2CppType().Name;
                    WeedInstance weedInstance =
                        itemInstance.TryCast<WeedInstance>();
                    if (weedInstance != null)
                    {
                        packagingId = weedInstance.PackagingID;
                    }
                    else
                    {
                        MethInstance methInstance =
                            itemInstance.TryCast<MethInstance>();
                        if (methInstance != null)
                        {
                            packagingId = methInstance.PackagingID;
                        }
                        else
                        {
                            CocaineInstance cocaineInstance =
                                itemInstance.TryCast<CocaineInstance>();
                            if (cocaineInstance != null)
                            {
                                packagingId = cocaineInstance.PackagingID;
                            }
                        }
                    }


                    switch (packagingId?.ToLower())
                    {
                        case "brick":
                            brickSlots.Add(itemSlot);
                            break;
                        case "jar":
                            jarSlots.Add(itemSlot);
                            break;
                        case "baggie":
                            unitSlots.Add(itemSlot);
                            break;
                        default:
                            unitSlots.Add(itemSlot);
                            break;
                    }
                }
            }

            uint remainingQuantity = totalQuantityNeeded;
            List<(ItemSlot slot, int quantityToRemove)> plannedChanges =
                new List<(ItemSlot, int)>();

            foreach (var slot in brickSlots)
            {
                if (remainingQuantity == 0)
                    break;
                uint itemsNeeded = remainingQuantity / 20;
                if (itemsNeeded == 0)
                    break;
                uint itemsAvailable = (uint)slot.Quantity;
                uint itemsToTake = Math.Min(itemsNeeded, itemsAvailable);

                if (itemsToTake > 0)
                {
                    plannedChanges.Add((slot, (int)itemsToTake));
                    remainingQuantity -= itemsToTake * 20;
                }
            }

            if (remainingQuantity > 0)
            {
                foreach (var slot in jarSlots)
                {
                    if (remainingQuantity == 0)
                        break;
                    uint itemsNeeded = remainingQuantity / 5;
                    if (itemsNeeded == 0)
                        break;
                    uint itemsAvailable = (uint)slot.Quantity;
                    uint itemsToTake = Math.Min(itemsNeeded, itemsAvailable);

                    if (itemsToTake > 0)
                    {
                        plannedChanges.Add((slot, (int)itemsToTake));
                        remainingQuantity -= itemsToTake * 5;
                    }
                }
            }

            if (remainingQuantity > 0)
            {
                foreach (var slot in unitSlots)
                {
                    if (remainingQuantity == 0)
                        break;
                    uint itemsNeeded = remainingQuantity;
                    uint itemsAvailable = (uint)slot.Quantity;
                    uint itemsToTake = Math.Min(itemsNeeded, itemsAvailable);

                    if (itemsToTake > 0)
                    {
                        plannedChanges.Add((slot, (int)itemsToTake));
                        remainingQuantity -= itemsToTake * 1;
                    }
                }
            }

            if (remainingQuantity == 0)
            {
                foreach (var change in plannedChanges)
                {
                    change.slot.ChangeQuantity(-change.quantityToRemove);
                }

                Il2CppSystem.Collections.Generic.List<ItemInstance> list =
                    new Il2CppSystem.Collections.Generic.List<ItemInstance>();
                ItemDefinition itemDef =
                    Registry.instance._GetItem(targetProductId);
                if (itemDef != null)
                {
                    ItemInstance handoverInstance = itemDef.GetDefaultInstance();
                    handoverInstance.Quantity = (int)totalQuantityNeeded;
                    list.Add(handoverInstance);

                    __instance.ProcessHandover(
                        HandoverScreen.EHandoverOutcome.Finalize,
                        __instance.CurrentContract, list, true);

                }
                else
                {

                    MelonLogger.Error(
                        $"HandoverChosenPrefix: Failed to get ItemDefinition for {targetProductId}. Cannot process handover.");
                }

                return false;
            }
            else
            {
                MelonLogger.Warning(
                    $"HandoverChosenPrefix: Failed to fulfill contract. {remainingQuantity} units still needed after planning. No changes applied to inventory.");
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(Il2CppScheduleOne.Dialogue.DialogueController))]
    class DialogTest
    {
        
        [HarmonyPatch("Interacted")]
        [HarmonyPrefix]
        static bool PreInteracted(DialogueController __instance)
        {
            try
            {
                if (!Config.SuperAutoHandover) return true;
                Customer customer = __instance.npc.gameObject.GetComponent<Customer>();
                float testvalue = customer.CurrentContract.Payment;
                if (customer.IsAtDealLocation())
                {
                    return HandleDeal.DoDeal(customer);
                }
                else
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                return true;
                //ignore
                // Its not a customer with a contract currently open
            }
        }
        
    }
    
    [HarmonyPatch(typeof(Il2CppScheduleOne.Economy.Customer))]
    class Handovers {
        [HarmonyPatch(typeof(Customer), "HandoverChosen")]
        [HarmonyPrefix]
        private static bool HandoverChosenPrefix(Customer __instance)
        {
            return HandleDeal.DoDeal(__instance);
        }
        
    }
    
    
    

    //thanks to johnnyjohnny_ and his 14 hours of helping me, what a chad
    [HarmonyPatch(typeof(Il2CppScheduleOne.UI.Phone.Messages.DealWindowSelector))]
    [HarmonyPatch("SetIsOpen", new[] { typeof(bool), typeof(MSGConversation), typeof(Il2CppSystem.Action<EDealWindow>) })]
    class InstaClick {
        [HarmonyPostfix]
        static void PostOpenWindow(Il2CppScheduleOne.UI.Phone.Messages.DealWindowSelector __instance, bool open, MSGConversation conversation, Action<EDealWindow> callback) {
            if (!Config.AutoScheduleDeals) return;
            if (Config.DealDayTime == 1)
            {
                __instance.MorningButton.Clicked();
            } else if (Config.DealDayTime == 2)
            {
                __instance.AfternoonButton.Clicked();
            } else if (Config.DealDayTime == 3)
            {
                __instance.NightButton.Clicked();
            }
            else
            {
                __instance.LateNightButton.Clicked();
            }
            
        }
    }

    [HarmonyPatch(typeof(Il2CppScheduleOne.AvatarFramework.Animation.AvatarLookController))]
    class AvatarLook {
        [HarmonyPatch("UpdateShit")]
        [HarmonyPostfix]
        static void AvatarLookController(Il2CppScheduleOne.AvatarFramework.Animation.AvatarLookController __instance)
        {
            try
            {
                if (__instance.nearestPlayerDist < 4f)
                {
                    if (!Config.MagicHandover) return;
                    NPC npc = __instance.NPC;
                    Customer customer = npc.gameObject.GetComponent<Customer>();
                    float testvalue = customer.CurrentContract.Payment;
                    if (customer.IsAtDealLocation() && customer.CurrentContract.enabled)
                    {
                        HandleDeal.DoDeal(customer);
                        customer.CurrentContract.enabled = false;
                        customer.CurrentContract.Finalize();
                    }
                    else
                    {
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                return;
            }
        }
    }
    
    /*
    [HarmonyPatch(typeof(Il2CppScheduleOne.Vision.VisionCone))]
    class VisionCone
    {
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void Update(Il2CppScheduleOne.Vision.VisionCone __instance)
        {
            if (__instance.IsPlayerVisible(Player.Local))
            {
                
                if (!Config.SuperAutoHandover) return;
                Customer customer = __instance.npc.gameObject.GetComponent<Customer>();
                float testvalue = customer.CurrentContract.Payment;
                if (customer.IsAtDealLocation())
                {
                    HandleDeal.DoDeal(customer);
                }
                else
                {
                    return;
                }
            }
        }
    }
    */

    
    
    
    

    [HarmonyPatch(typeof(Il2CppScheduleOne.Quests.Quest))]
    class QuestTest
    {
        static HashSet<int> handledQuests = new HashSet<int>();

        
        [HarmonyPatch("SetIsTracked")]
        [HarmonyPostfix]
        static void SetIsTrackedPrefix(Il2CppScheduleOne.Quests.Quest __instance, bool tracked)
        {
            //
            if (!tracked) return;
            if (Config.AutoTrack) return;
            if (!__instance.title.Contains("Deal for")) return;
            int questId = __instance.GetHashCode();
            if (!handledQuests.Contains(questId))
            {
                handledQuests.Add(questId);
                __instance.SetIsTracked(false);
                return;
            }
            return;
        }
    }
    
    /*
    [HarmonyPatch(typeof(Il2CppScheduleOne.UI.Phone.Messages.MessagesApp))]
    class MainLoop
    {
        
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        private static void Postfix(Il2CppScheduleOne.UI.Phone.Messages.MessagesApp __instance)
        {
            Response? r1=null;
            Response? r2=null;
            
            MSGConversation? answerto=null;
            if (__instance.unreadConversations.Count==0) return;
            var instanceUnreadConversation = __instance.unreadConversations[0];
            if (instanceUnreadConversation.contactName=="Beth Penn") instanceUnreadConversation.SetRead(true);
            if (instanceUnreadConversation.currentResponses.Count==0)instanceUnreadConversation.SetRead(true);
            r1 = instanceUnreadConversation.currentResponses[0];
            r2 = instanceUnreadConversation.currentResponses[1];
            
            if (instanceUnreadConversation.bubbles[instanceUnreadConversation.bubbles.Count - 1].alignment == MessageBubble.Alignment.Left)
            {
                    answerto=instanceUnreadConversation;
            }
            
            if (!(answerto is null))
            {
                if (r2 != null && r2.label=="COUNTEROFFER")answerto.ResponseChosen(r2, true);
                if (r1 != null && r1.label=="ACCEPT_CONTRACT")answerto.ResponseChosen(r1, true);
            }
        }
            
    }
    */


    [HarmonyPatch(typeof(Il2CppScheduleOne.Messaging.MSGConversation))]
    class MsgTest
    {
        /*
        [HarmonyPatch(nameof(Il2CppScheduleOne.Messaging.MSGConversation.MoveToTop))]
        [HarmonyPrefix]
        public static void MoveToTop(Il2CppScheduleOne.Messaging.MSGConversation __instance)
        {
            MelonLogger.Msg("MoveToTop");
            MelonLogger.Msg(__instance.currentResponses.Count);
        }
        */
        [HarmonyPatch(nameof(Il2CppScheduleOne.Messaging.MSGConversation.RefreshPreviewText))]
        [HarmonyPostfix]
        public static void RefreshPreviewText(Il2CppScheduleOne.Messaging.MSGConversation __instance)
        {
            if (__instance.currentResponses.Count >= 2 && __instance.bubbles[__instance.bubbles.Count - 1].alignment == MessageBubble.Alignment.Left)
            {
                if (__instance.currentResponses[1].label == "COUNTEROFFER" && Config.AutoCounterOffer)
                {
                    __instance.ResponseChosen(__instance.currentResponses[1],true);
                }
                else if (__instance.currentResponses[0].label == "ACCEPT_CONTRACT" && Config.AutoScheduleDeals)
                {
                    __instance.ResponseChosen(__instance.currentResponses[0],true);
                }
            }
        }
    }
    
    
    [HarmonyPatch(typeof(Il2CppScheduleOne.UI.Phone.CounterofferInterface))]
    class CounterofferInterfacePatches
    {
        private static void UpdateConfirmButtonText(Il2CppScheduleOne.UI.Phone.CounterofferInterface instance)
        {
            if (!Config.AutoCounterOffer) return;
            if (!instance.IsOpen) return;
            float chance = EntryPoint.EvaluateCounterofferPercentage(
                instance.selectedProduct,
                instance.quantity,
                instance.price,
                instance.conversation.sender.GetComponent<Customer>()
            );
            if (chance < 70f)
            {
                instance.conversation.ResponseChosen(instance.conversation.currentResponses[0], true);
                return;
            }
            while (Mathf.RoundToInt(chance) == 100)
            {
                instance.price += 1;
                chance = EntryPoint.EvaluateCounterofferPercentage(
                    instance.selectedProduct,
                    instance.quantity,
                    instance.price,
                    instance.conversation.sender.GetComponent<Customer>()
                );
            }
            instance.price -= 1;
            instance.PriceInput.text=instance.price.ToString();
            instance.Update();
            instance.Send();
            
            instance.IsOpen=false;
            
        }
        
        
        [HarmonyPatch("Open")]
        [HarmonyPostfix]
        static void PostOpenPatch(Il2CppScheduleOne.UI.Phone.CounterofferInterface __instance)
        {
            UpdateConfirmButtonText(__instance);
        }

        [HarmonyPatch("ChangePrice")]
        [HarmonyPostfix]
        static void PostPriceChangePatch(Il2CppScheduleOne.UI.Phone.CounterofferInterface __instance)
        {
            UpdateConfirmButtonText(__instance);
        }

        [HarmonyPatch("ChangeQuantity")]
        [HarmonyPostfix]
        static void PostQuantityChangePatch(Il2CppScheduleOne.UI.Phone.CounterofferInterface __instance)
        {
            UpdateConfirmButtonText(__instance);
        }
    }
}