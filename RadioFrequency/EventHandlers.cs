using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using RadioFrequency.Features;
using RueI.Displays;
using RueI.Elements;
using RueI.Extensions;
using UserSettings.ServerSpecific;

namespace RadioFrequency
{
    internal static class EventHandlers
    {
        internal static void RegisterEvents()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers += OnWaitingForPlayers;
            Exiled.Events.Handlers.Player.Left += OnLeft;
            Exiled.Events.Handlers.Player.ItemAdded += OnItemAdded;
            Exiled.Events.Handlers.Player.ItemRemoved += OnItemRemoved;
            ServerSpecificSettingsSync.ServerOnSettingValueReceived += OnSettingValueReceived;
        }

        internal static void UnregisterEvents()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers -= OnWaitingForPlayers;
            Exiled.Events.Handlers.Player.Left -= OnLeft;
            Exiled.Events.Handlers.Player.ItemAdded -= OnItemAdded;
            Exiled.Events.Handlers.Player.ItemRemoved -= OnItemRemoved;
            ServerSpecificSettingsSync.ServerOnSettingValueReceived -= OnSettingValueReceived;
        }

        private static void OnWaitingForPlayers()
        {
            Frequency.RadioFrequency.Clear();

            foreach (Frequency frequency in Frequency.Frequencies)
            {
                frequency.Players.Clear();
            }
        }

        private static void OnLeft(LeftEventArgs ev)
        {
            if (Frequency.TryGetPlayerFrequency(ev.Player, out Frequency frequency))
                frequency.RemovePlayer(ev.Player);
        }

        private static void OnItemAdded(ItemAddedEventArgs ev)
        {
            if (ev.Item == null || ev.Item.Type != ItemType.Radio)
                return;

            if (Frequency.TryGetRadioFrequency(ev.Item.Serial, out Frequency radioFrequency) && (radioFrequency.AuthorizedRoles.Contains(ev.Player.Role.Type) || radioFrequency.CanBePickedUp))
            {
                radioFrequency.AddPlayer(ev.Player);
            }
            else
            {
                if (!Frequency.TryGetFrequenciesByRole(ev.Player.Role.Type, out List<Frequency> frequencies) || frequencies.Count <= 0)
                    return;

                Frequency frequency = frequencies.First();
                frequency.AddPlayer(ev.Player);

                Frequency.SetRadioFrequency(ev.Item.Serial, frequency);
            }
        }

        private static void OnItemRemoved(ItemRemovedEventArgs ev)
        {
            if (ev.Item == null || ev.Item.Type != ItemType.Radio || ev.Player.Items.Any(i => i.Type == ItemType.Radio))
                return;

            if (Frequency.TryGetPlayerFrequency(ev.Player, out Frequency frequency))
                frequency.RemovePlayer(ev.Player);
        }

        private static void OnSettingValueReceived(ReferenceHub hub, ServerSpecificSettingBase settingBase)
        {
            if (settingBase is not SSKeybindSetting keybindSetting || keybindSetting.SettingId != Plugin.Singleton.Config.KeybindId || !keybindSetting.SyncIsPressed)
                return;

            if (!Player.TryGet(hub, out Player player)) 
                return;

            DisplayCore core = DisplayCore.Get(hub);
            SetElement element;

            if (player.Items.All(i => i.Type != ItemType.Radio))
            {
                element = new SetElement(500, Plugin.Singleton.Config.NoRadioHint);
                core.AddTemp(element, TimeSpan.FromSeconds(2), new RueI.Displays.Scheduling.TimedElemRef<SetElement>());
                return;
            }

            if (!Frequency.TryGetPlayerFrequency(player, out Frequency frequency))
                return;

            Frequency nextFrequency = Frequency.GetNextFrequency(player.Role.Type, frequency);

            frequency.RemovePlayer(player);
            nextFrequency.AddPlayer(player);

            foreach (Item playerItem in player.Items)
            {
                if (playerItem.Type == ItemType.Radio)
                {
                    Frequency.SetRadioFrequency(playerItem.Serial, nextFrequency);
                }
            }

            element = new SetElement(500, Plugin.Singleton.Config.ChangedFrequencyHint.Replace("{radio_frequency}", nextFrequency.Name));
            core.AddTemp(element, TimeSpan.FromSeconds(2), new RueI.Displays.Scheduling.TimedElemRef<SetElement>());
        }
    }
}