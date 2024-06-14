namespace miniExplorer.Properties
{
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "16.3.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase
    {

        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));

        public static Settings Default
        {
            get
            {
                return defaultInstance;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("100, 100")]
        public global::System.Drawing.Point WindowLocation
        {
            get
            {
                return ((global::System.Drawing.Point)(this["WindowLocation"]));
            }
            set
            {
                this["WindowLocation"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("600, 500")]
        public global::System.Drawing.Size UnfoldedSize
        {
            get
            {
                return ((global::System.Drawing.Size)(this["UnfoldedSize"]));
            }
            set
            {
                this["UnfoldedSize"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("100")]
        public int FavoritesPanelWidth
        {
            get
            {
                return ((int)(this["FavoritesPanelWidth"]));
            }
            set
            {
                this["FavoritesPanelWidth"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int SelectedIndex
        {
            get
            {
                return ((int)(this["SelectedIndex"]));
            }
            set
            {
                this["SelectedIndex"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public global::System.Collections.Generic.List<string> TabPaths
        {
            get
            {
                return ((global::System.Collections.Generic.List<string>)(this["TabPaths"]));
            }
            set
            {
                this["TabPaths"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public global::System.Collections.Generic.List<string> Favorites
        {
            get
            {
                return ((global::System.Collections.Generic.List<string>)(this["Favorites"]));
            }
            set
            {
                this["Favorites"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("All")]
        public global::System.Windows.Forms.DragDropEffects DragEffect
        {
            get
            {
                return ((global::System.Windows.Forms.DragDropEffects)(this["DragEffect"]));
            }
            set
            {
                this["DragEffect"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("All")]
        public global::System.Windows.Forms.DragDropEffects DropEffect
        {
            get
            {
                return ((global::System.Windows.Forms.DragDropEffects)(this["DropEffect"]));
            }
            set
            {
                this["DropEffect"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("false")]
        public bool ShowHidden
        {
            get
            {
                return ((bool)(this["ShowHidden"]));
            }
            set
            {
                this["ShowHidden"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("false")]
        public bool ShowFavorites
        {
            get
            {
                return ((bool)(this["ShowFavorites"]));
            }
            set
            {
                this["ShowFavorites"] = value;
            }
        }
    }
}
