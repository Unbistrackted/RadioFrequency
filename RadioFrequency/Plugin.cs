using System;
using System.Collections.Generic;
using Exiled.API.Features;
using Exiled.API.Features.Core.UserSettings;
using HarmonyLib;
using PlayerRoles;
using RadioFrequency.Features;
using RueI;

namespace RadioFrequency
{
    public class Plugin : Plugin<Config>
    {
        private Harmony _harmony;
        private IEnumerable<SettingBase> _settings;

        public override string Name { get; } = "RadioFrequency";
        public override string Author { get; } = "Bolton";
        public override Version Version { get; } = new(1, 0, 1);
        public override Version RequiredExiledVersion { get; } = new(9, 0, 0);

        public static Plugin Singleton { get; private set; }

        public override void OnEnabled()
        {
            Singleton = this;
            _harmony = new Harmony("fr.bolton.radiofrequency");
            _harmony.PatchAll();
            RueIMain.EnsureInit();

            if (Config.UseDefaultRadio)
            {
                Frequency defaultFrequency = new(Config.DefaultRadioName, new HashSet<RoleTypeId>(), true);
                defaultFrequency.Init();
            }

            foreach (Frequency frequency in Config.Frequencies)
            {
                frequency.Init();
            }

            _settings = new SettingBase[]
            {
                new HeaderSetting(Config.SettingHeaderLabel),
                new KeybindSetting(Config.KeybindId, Config.KeybindLabel, default, hintDescription: Config.KeybindHint),
            };
            SettingBase.Register(_settings);

            EventHandlers.RegisterEvents();

            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            Singleton = null;
            _harmony.UnpatchAll(_harmony.Id);
            _harmony = null;

            SettingBase.Unregister(null, _settings);
            _settings = null;

            EventHandlers.UnregisterEvents();

            base.OnDisabled();
        }
    }
}