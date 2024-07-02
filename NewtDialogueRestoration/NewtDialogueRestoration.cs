using BepInEx;
using R2API;
using RoR2;
using System.Globalization;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace NewtDialogueRestoration
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class NewtDialogueRestoration : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "SSM24";
        public const string PluginName = "NewtDialogueRestoration";
        public const string PluginVersion = "1.0.0";

        // The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            // Init our logging class so that we can properly log for debugging
            Log.Init(Logger);

            On.EntityStates.NewtMonster.KickFromShop.OnEnter += On_KickFromShop_OnEnter;
            On.RoR2.PurchaseInteraction.OnInteractionBegin += On_PurchaseInteraction_OnInteractionBegin;

            // apparently this gets called already, but the method is broken so it just puts a blank line in chat
            // replace it with a blank method to prevent it from running
            On.RoR2.BazaarController.CommentOnLunarPurchase += (_, _) => { };
        }

        private void On_KickFromShop_OnEnter(On.EntityStates.NewtMonster.KickFromShop.orig_OnEnter orig, EntityStates.NewtMonster.KickFromShop self)
        {
            orig(self);
            Log.Debug("Running On_KickFromShop_OnEnter...");
            SendNewtMessage("NEWT_ANNOY", 6, 100f);
        }

        private void On_PurchaseInteraction_OnInteractionBegin(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, 
            PurchaseInteraction self, Interactor activator)
        {
            orig(self, activator);
            if (BazaarController.instance)
            {
                string name = self.gameObject.name;
                if (name.StartsWith("LunarShopTerminal"))
                {
                    SendNewtMessage("NEWT_LUNAR_PURCHASE", 8, 100f);
                }
                else if (name.StartsWith("LunarRecycler"))
                {
                    SendNewtMessage("NEWT_UPGRADE", 3, 0f);
                }
                else if (name.StartsWith("LunarCauldron"))
                {
                    SendNewtMessage("NEWT_UPGRADE", 3, 100f);
                }
            }
        }

        private static void SendNewtMessage(string token, int tokenCount, float percentChance)
        {
            if (!NetworkServer.active)
            {
                return;
            }

            CharacterBody newtBody = GetNewtBody();
            if (newtBody != null && Util.CheckRoll(percentChance))
            {
                // there are probably better ways to do this but i genuinely could not find one :v
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                {
                    baseToken = string.Format(CultureInfo.InvariantCulture, "<color=#b0e5ff><size=120%>{0}: {1}</color></size>",
                        Util.GetBestBodyName(newtBody.gameObject),
                        Language.GetString(token + "_" + Random.Range(1, tokenCount)))
                });
            }
        }

        private static CharacterBody GetNewtBody()
        {
            // there are probably better ways to do this too but yea
            foreach (var teamComponent in TeamComponent.GetTeamMembers(TeamIndex.Neutral))
            {
                if (teamComponent.name.StartsWith("ShopkeeperBody"))
                {
                    return teamComponent.body;
                }
            }
            return null;
        }
    }
}
