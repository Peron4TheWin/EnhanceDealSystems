using MelonLoader;
using HarmonyLib;
using Il2CppScheduleOne;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Product;
using UnityEngine;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Messaging;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.UI.Handover;
using Il2CppScheduleOne.UI.Phone.Messages;

[assembly: MelonInfo(typeof(EnhanceDealSystems.EntryPoint), "EnhanceDealSystems", "1.0.0", "_peron")]

namespace EnhanceDealSystems
{
    public class EntryPoint : MelonMod
    {
        public HarmonyLib.Harmony harmony;

        public override void OnInitializeMelon()
        {
            harmony = new HarmonyLib.Harmony("com.peron.EnhanceDealSystems");
            harmony.PatchAll();
        }


        public static float EvaluateCounterofferPercentage(ProductDefinition product, int quantity, float price,
            Customer customer)
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


    //thanks to johnnyjohnny_ and his 14 hours of helping me, what a chad
    [HarmonyPatch(typeof(Il2CppScheduleOne.UI.Phone.Messages.DealWindowSelector))]
    [HarmonyPatch("SetIsOpen", new[] { typeof(bool), typeof(MSGConversation), typeof(Il2CppSystem.Action<EDealWindow>) })]
    class InstaClick {
        [HarmonyPostfix]
        static void PostOpenWindow(Il2CppScheduleOne.UI.Phone.Messages.DealWindowSelector __instance, bool open, MSGConversation conversation, Action<EDealWindow> callback) {
            __instance.LateNightButton.Clicked();
        }
    }
    
    [HarmonyPatch(typeof(Il2CppScheduleOne.Economy.Customer))]
    class handovers
    {
        [HarmonyPatch("HandoverChosen")]
        [HarmonyPrefix]
        private static bool HandoverChosenPrefix(Il2CppScheduleOne.Economy.Customer __instance)
        {
            Il2CppSystem.Collections.Generic.List<ItemInstance> list = new Il2CppSystem.Collections.Generic.List<ItemInstance>();
            string productid = "";

            if (__instance.CurrentContract?.ProductList?.entries != null)
            {
                foreach (ProductList.Entry productListEntry in __instance.CurrentContract.ProductList.entries)
                {
                    if (productListEntry?.Pointer != null)
                    {
                        productid = productListEntry.ProductID;
                        //Thanks to stupidrepo for this
                        list.Add(Registry.instance._GetItem(productListEntry.ProductID).GetDefaultInstance());
                    }
                }
            }
            else
            {
                return true;
            }

            uint quantity = (uint)__instance.CurrentContract.ProductList.entries[0].Quantity;
            //Thanks maxtorcoder and not.rau for explaining Singleton's to me :D 
            uint playerquantityproduct = PlayerSingleton<PlayerInventory>.Instance.GetAmountOfItem(__instance.CurrentContract.ProductList.entries[0].ProductID);
            if (playerquantityproduct >= quantity)
            {
                PlayerSingleton<PlayerInventory>.Instance.RemoveAmountOfItem(productid, quantity);
                __instance.ProcessHandover(HandoverScreen.EHandoverOutcome.Finalize, __instance.CurrentContract, list,
                    true);
            }

            return false;
        }
    }
    
    


    [HarmonyPatch(typeof(Il2CppScheduleOne.UI.Phone.Messages.MessagesApp))]
    class MainLoop
    {
        
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        private static void Postfix(Il2CppScheduleOne.UI.Phone.Messages.MessagesApp __instance)
        {
            try
            {
                /* Need to make the mod able to automatically send messages for only owner bc idk what would happen if multiple people use this mod at the time
                if (Player.Local.IsHost == false)
                {
                    return;
                }
                */  
            
                string word = "";
                foreach (var instanceUnreadConversation in __instance.unreadConversations)
                {
                    if (instanceUnreadConversation.currentResponses.Count == 3)
                    {
                        word = "COUNTEROFFER";
                    } else if (instanceUnreadConversation.currentResponses.Count == 2)
                    {
                        if (instanceUnreadConversation.messageHistory[instanceUnreadConversation.messageHistory.Count-1].sender == Message.ESenderType.Player) continue;
                        word = "ACCEPT_CONTRACT";
                    }
                
                    
                    foreach (Response currentResponse in instanceUnreadConversation.currentResponses)
                    {
                        //left = npc
                        //right = you
                        //if (instanceUnreadConversation.bubbles[instanceUnreadConversation.bubbles.Count-1].alignment==1)
                        if (currentResponse.label.Equals(word) && instanceUnreadConversation.bubbles[instanceUnreadConversation.bubbles.Count-1].alignment==MessageBubble.Alignment.Left)
                        {
                            instanceUnreadConversation.ResponseChosen(currentResponse, true); }
                    }
                }
            }
            catch (Exception e)
            {
                //ignore
            }
        }
            
    }
    

[HarmonyPatch(typeof(Il2CppScheduleOne.UI.Phone.CounterofferInterface))]
    class CounterofferInterfacePatches
    {
        private static void UpdateConfirmButtonText(Il2CppScheduleOne.UI.Phone.CounterofferInterface instance)
        {
            if (!instance.IsOpen) return;
            float chance = EntryPoint.EvaluateCounterofferPercentage(
                instance.selectedProduct,
                instance.quantity,
                instance.price,
                instance.conversation.sender.GetComponent<Customer>()
            );
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