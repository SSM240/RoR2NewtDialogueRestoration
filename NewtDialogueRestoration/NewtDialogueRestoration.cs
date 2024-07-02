using BepInEx;
using BepInEx.Configuration;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;

namespace NewtDialogueRestoration
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency("com.rune580.riskofoptions")]
    public class NewtDialogueRestoration : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "SSM24";
        public const string PluginName = "NewtDialogueRestoration";
        public const string PluginVersion = "1.0.0";

        public static ConfigEntry<float> PurchaseDialogueChance;
        public static ConfigEntry<float> AnnoyDialogueChance;
        public static ConfigEntry<float> UpgradeDialogueOnRerollChance;
        public static ConfigEntry<float> UpgradeDialogueOnCauldronChance;

        public void Awake()
        {
            Log.Init(Logger);

            PurchaseDialogueChance = Config.Bind("Chances", "Purchase Dialogue", 40f,
                "Chance for dialogue on purchasing a lunar item");
            AnnoyDialogueChance = Config.Bind("Chances", "Annoy Dialogue", 100f,
                "Chance for dialogue on getting kicked out of the shop");
            UpgradeDialogueOnRerollChance = Config.Bind("Chances", "Upgrade Dialogue On Reroll", 0f,
                "Chance for \"upgrade\" dialogue on rerolling lunar items");
            UpgradeDialogueOnCauldronChance = Config.Bind("Chances", "Upgrade Dialogue On Cauldron", 0f,
                "Chance for \"upgrade\" dialogue on using a lunar cauldron");

            ModSettingsManager.AddOption(new SliderOption(PurchaseDialogueChance));
            ModSettingsManager.AddOption(new SliderOption(AnnoyDialogueChance));
            ModSettingsManager.AddOption(new SliderOption(UpgradeDialogueOnRerollChance));
            ModSettingsManager.AddOption(new SliderOption(UpgradeDialogueOnCauldronChance));

            On.EntityStates.NewtMonster.KickFromShop.OnEnter += On_KickFromShop_OnEnter;
            On.RoR2.PurchaseInteraction.OnInteractionBegin += On_PurchaseInteraction_OnInteractionBegin;
            // apparently this gets called already, but the method is broken so it just puts a blank line in chat
            // replace it with a blank method to prevent it from running
            On.RoR2.BazaarController.CommentOnLunarPurchase += (_, _) => { };
        }

        private void On_KickFromShop_OnEnter(On.EntityStates.NewtMonster.KickFromShop.orig_OnEnter orig, EntityStates.NewtMonster.KickFromShop self)
        {
            orig(self);
            SendNewtMessage("NEWT_ANNOY", 6, AnnoyDialogueChance.Value);
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
                    SendNewtMessage("NEWT_LUNAR_PURCHASE", 8, PurchaseDialogueChance.Value);
                }
                else if (name.StartsWith("LunarRecycler"))
                {
                    SendNewtMessage("NEWT_UPGRADE", 3, UpgradeDialogueOnRerollChance.Value);
                }
                else if (name.StartsWith("LunarCauldron"))
                {
                    SendNewtMessage("NEWT_UPGRADE", 3, UpgradeDialogueOnCauldronChance.Value);
                }
            }
        }

        private static int lastRolledIndex;

        private static void SendNewtMessage(string token, int tokenCount, float percentChance)
        {
            if (!NetworkServer.active)
            {
                return;
            }

            CharacterBody newtBody = GetNewtBody();
            if (newtBody != null && Util.CheckRoll(percentChance))
            {
                int index;
                do
                {
                    index = Random.Range(1, tokenCount);
                } while (index == lastRolledIndex); // prevent same line from being used twice in a row

                // there are probably better ways to do this but i genuinely could not find one :v
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                {
                    baseToken = string.Format(CultureInfo.InvariantCulture, "<color=#b0e5ff><size=120%>{0}: {1}</color></size>",
                        Util.GetBestBodyName(newtBody.gameObject),
                        Language.GetString(token + "_" + index))
                });
                lastRolledIndex = index;
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
