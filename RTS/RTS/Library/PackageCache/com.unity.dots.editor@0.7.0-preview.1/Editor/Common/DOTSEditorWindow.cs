using System;
using System.Text.RegularExpressions;
using Unity.Assertions;
using Unity.Serialization.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Entities.Editor
{
    abstract class DOTSEditorWindow : EditorWindow
    {
        static readonly string k_NoWorldString = L10n.Tr("No World");

        readonly WorldsChangeDetector m_WorldsChangeDetector = new WorldsChangeDetector();

        ToolbarMenu m_WorldSelector;
        VisualElement m_SearchFieldContainer;
        ToolbarSearchField m_SearchField;
        Image m_SearchIcon;
        bool m_PreviousShowAdvancedWorldsValue;

        BaseStateContainer m_BaseState;
        string BaseStateKey => $"{GetType().Name}.{nameof(BaseStateContainer)}";
        BaseStateContainer BaseState => m_BaseState ?? (m_BaseState = SessionState<BaseStateContainer>.GetOrCreateState(BaseStateKey));

        class BaseStateContainer
        {
            public string SelectedWorldName;
            public string SearchFilter;
        }

        protected string SearchFilter
        {
            get => BaseState.SearchFilter;
            set
            {
                BaseState.SearchFilter = value;

                UIElementHelper.Show(m_SearchField);
                m_SearchField.Q("unity-text-input").Focus();

                m_SearchField.value = value;
            }
        }

        protected World GetCurrentlySelectedWorld()
        {
            if (World.All.Count == 0)
            {
                return null;
            }

            World selectedWorld = null;
            foreach (var world in World.All)
            {
                if (world.Name == BaseState.SelectedWorldName)
                {
                    selectedWorld = world;
                    break;
                }
            }

            if (null == selectedWorld)
            {
                selectedWorld = World.All[0];
            }

            BaseState.SelectedWorldName = selectedWorld.Name;
            return selectedWorld;
        }

        protected ToolbarMenu CreateWorldSelector()
        {
            m_WorldSelector = new ToolbarMenu
            {
                name = "worldMenu",
                variant = ToolbarMenu.Variant.Popup
            };

            UpdateWorldDropDownMenu();

            m_PreviousShowAdvancedWorldsValue = UserSettings<AdvancedSettings>.GetOrCreate(Constants.Settings.AdvancedSettings).ShowAdvancedWorlds;

            return m_WorldSelector;
        }

        protected void AddSearchFieldContainer(VisualElement parent, string ussClass)
        {
            m_SearchFieldContainer = new VisualElement();
            m_SearchFieldContainer.AddToClassList(ussClass);

            CreateSearchField(UssClasses.DotsEditorCommon.SearchField);
            m_SearchFieldContainer.Add(m_SearchField);

            parent.Add(m_SearchFieldContainer);
        }

        void CreateSearchField(string ussClass)
        {
            m_SearchField = new ToolbarSearchField
            {
                value = string.IsNullOrEmpty(BaseState.SearchFilter) ? string.Empty : BaseState.SearchFilter
            };
            m_SearchField.AddToClassList(ussClass);
            m_SearchField.Q("unity-cancel").AddToClassList(UssClasses.DotsEditorCommon.SearchFieldCancelButton);
            m_SearchField.RegisterValueChangedCallback(OnFilterChanged);
            UIElementHelper.Hide(m_SearchField);
        }

        protected void AddSearchIcon(VisualElement parent, string ussClass)
        {
            var searchIconContainer = new VisualElement();
            searchIconContainer.AddToClassList(UssClasses.DotsEditorCommon.SearchIconContainer);

            m_SearchIcon = new Image();
            Resources.Templates.DotsEditorCommon.AddStyles(m_SearchIcon);
            m_SearchIcon.AddToClassList(UssClasses.DotsEditorCommon.CommonResources);
            m_SearchIcon.AddToClassList(ussClass);

            m_SearchIcon.RegisterCallback<MouseUpEvent>(evt =>
            {
                if (UIElementHelper.IsVisible(m_SearchField))
                {
                    UIElementHelper.Hide(m_SearchField);
                }
                else
                {
                    UIElementHelper.Show(m_SearchField);
                    m_SearchField.Q("unity-text-input").Focus();
                }
            });

            searchIconContainer.Add(m_SearchIcon);
            parent.Add(searchIconContainer);
        }

        protected ToolbarMenu CreateDropDownSettings(string ussClass)
        {
            var dropdownSettings = new ToolbarMenu()
            {
                name = "dropdownSettings",
                variant = ToolbarMenu.Variant.Popup
            };

            Resources.Templates.DotsEditorCommon.AddStyles(dropdownSettings);
            dropdownSettings.AddToClassList(UssClasses.DotsEditorCommon.CommonResources);
            dropdownSettings.AddToClassList(ussClass);

            var arrow = dropdownSettings.Q(className: "unity-toolbar-menu__arrow");
            arrow.style.backgroundImage = null;

            return dropdownSettings;
        }

        public void Update()
        {
            if (NeedToChangeWorldDropDownMenu())
            {
                UpdateWorldDropDownMenu();
                OnWorldSelected(GetCurrentlySelectedWorld());
            }

            OnUpdate();
        }

        bool NeedToChangeWorldDropDownMenu()
        {
            if (null == m_WorldSelector)
                return false;

            if (m_WorldsChangeDetector.WorldsChanged())
                return true;

            var showAdvancedWorlds = UserSettings<AdvancedSettings>.GetOrCreate(Constants.Settings.AdvancedSettings).ShowAdvancedWorlds;
            if (m_PreviousShowAdvancedWorldsValue != showAdvancedWorlds)
            {
                m_PreviousShowAdvancedWorldsValue = showAdvancedWorlds;
                return true;
            }

            return false;
        }

        protected void UpdateWorldDropDownMenu()
        {
            Assert.IsNotNull(m_WorldSelector);

            var menu = m_WorldSelector.menu;
            var menuItemsCount = menu.MenuItems().Count;

            for (var i = 0; i < menuItemsCount; i++)
            {
                menu.RemoveItemAt(0);
            }

            var advancedSettings = UserSettings<AdvancedSettings>.GetOrCreate(Constants.Settings.AdvancedSettings);

            if (World.All.Count > 0)
            {
                AppendWorldMenu(menu, advancedSettings.ShowAdvancedWorlds);
            }
            else
            {
                menu.AppendAction(k_NoWorldString, OnWorldSelected, DropdownMenuAction.AlwaysEnabled);
            }

            var currentWorld = GetCurrentlySelectedWorld();
            m_WorldSelector.text = currentWorld == null ? k_NoWorldString : currentWorld.Name;
        }

        void AppendWorldMenu(DropdownMenu menu, bool showAdvancedWorlds)
        {
            var worldCategories = WorldCategoryHelper.Categories;

            foreach (var category in worldCategories)
            {
                if (showAdvancedWorlds)
                {
                    menu.AppendAction(category.Name.ToUpper(), null, DropdownMenuAction.Status.Disabled);
                    AppendWorlds(menu, category);
                    menu.AppendSeparator();
                }
                else if (category.Flag == WorldFlags.Live)
                {
                    AppendWorlds(menu, category);
                    break;
                }
            }
        }

        void AppendWorlds(DropdownMenu menu, WorldCategoryHelper.Category category)
        {
            foreach (var world in category.Worlds)
            {
                menu.AppendAction(world.Name, OnWorldSelected, a =>
                    (BaseState.SelectedWorldName == world.Name)
                    ? DropdownMenuAction.Status.Checked
                    : DropdownMenuAction.Status.Normal, world);
            }
        }

        void OnWorldSelected(DropdownMenuAction action)
        {
            var world = action.userData as World;
            if (world != null)
            {
                m_WorldSelector.text = world.Name;
                BaseState.SelectedWorldName = world.Name;
            }
            else
            {
                m_WorldSelector.text = k_NoWorldString;
            }

            OnWorldSelected(world);
        }

        protected void AddStringToSearchField(string toAdd)
        {
            if (!string.IsNullOrEmpty(SearchFilter)
                && SearchFilter.IndexOf(toAdd, StringComparison.OrdinalIgnoreCase) >= 0)
                return;

            SearchFilter += string.IsNullOrEmpty(SearchFilter)
                ? toAdd + " "
                : " " + toAdd + " ";
        }

        protected void RemoveStringFromSearchField(string toRemove)
        {
            if (string.IsNullOrEmpty(SearchFilter))
                return;

            SearchFilter = Regex.Replace(SearchFilter, toRemove, string.Empty, RegexOptions.IgnoreCase).Trim();
        }

        void OnFilterChanged(ChangeEvent<string> evt)
        {
            BaseState.SearchFilter = evt.newValue;
            OnFilterChanged(evt.newValue);
        }

        protected abstract void OnUpdate();
        protected abstract void OnWorldSelected(World world);
        protected abstract void OnFilterChanged(string filter);
    }
}
