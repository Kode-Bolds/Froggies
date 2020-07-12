using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder.EditorTests
{
    // We use a modified version of the EditorContextualMenuManager to avoid popping
    // a system menu, which prevent the test from continuing.
    class BuilderTestContextualMenuManager : ContextualMenuManager
    {
        public bool menuIsDisplayed { get; private set; }
        public int menuItemCount { get; private set; }

        DropdownMenu mMenu;

        public List<DropdownMenuItem> menuItems => mMenu.MenuItems();

        public DropdownMenuAction FindMenuAction(string name)
        {
            foreach (var menuItem in mMenu.MenuItems())
            {
                if (menuItem is DropdownMenuAction menuAction)
                {
                    if (menuAction.name == name)
                        return menuAction;
                }
            }

            return null;
        }

        protected internal override void DoDisplayMenu(DropdownMenu menu, EventBase triggerEvent)
        {
            menuIsDisplayed = true;
            menuItemCount = menu.MenuItems().Count;
            mMenu = menu;
        }

        public void SimulateItemSelection(int itemIndex, EventBase e)
        {
            List<DropdownMenuItem> items = mMenu.MenuItems();
            var action = items[itemIndex] as DropdownMenuAction;
            if (action != null)
            {
                action.Execute();
            }
        }

        public override void DisplayMenuIfEventMatches(EventBase evt, IEventHandler eventHandler)
        {
            if (evt == null)
            {
                return;
            }

            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                if (evt.eventTypeId == MouseDownEvent.TypeId())
                {
                    MouseDownEvent e = evt as MouseDownEvent;

                    if (e.button == (int)MouseButton.RightMouse ||
                        (e.button == (int)MouseButton.LeftMouse && e.modifiers == EventModifiers.Control))
                    {
                        DisplayMenu(evt, eventHandler);
                        evt.StopPropagation();
                        return;
                    }
                }
            }
            else
            {
                if (evt.eventTypeId == MouseUpEvent.TypeId())
                {
                    MouseUpEvent e = evt as MouseUpEvent;
                    if (e.button == (int)MouseButton.RightMouse)
                    {
                        DisplayMenu(evt, eventHandler);
                        evt.StopPropagation();
                        return;
                    }
                }
            }

            if (evt.eventTypeId == KeyUpEvent.TypeId())
            {
                KeyUpEvent e = evt as KeyUpEvent;
                if (e.keyCode == KeyCode.Menu)
                {
                    DisplayMenu(evt, eventHandler);
                    evt.StopPropagation();
                }
            }
        }
    }
}
