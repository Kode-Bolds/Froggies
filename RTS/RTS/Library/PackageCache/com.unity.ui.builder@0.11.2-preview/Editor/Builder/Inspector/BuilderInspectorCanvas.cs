using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UI.Builder
{
    internal class BuilderInspectorCanvas : IBuilderInspectorSection
    {
        static readonly int s_CameraRefreshDelayMS = 100;

        public VisualElement root => m_CanvasInspector;

        BuilderInspector m_Inspector;
        BuilderDocument m_Document;
        BuilderCanvas m_Canvas;
        VisualElement m_CanvasInspector;

        BuilderDocumentSettings settings => m_Document.settings;

        VisualElement customBackgroundElement => m_Canvas.customBackgroundElement;

        // Fields
        IntegerField m_CanvasWidth;
        IntegerField m_CanvasHeight;
        PercentSlider m_OpacityField;
        ToggleButtonStrip m_BackgroundMode;
        ColorField m_ColorField;
        ObjectField m_ImageField;
        ToggleButtonStrip m_ImageScaleModeField;
        Button m_FitCanvasToImageButton;
        ObjectField m_CameraField;

        bool m_CameraModeEnabled;
        RenderTexture m_InGamePreviewRenderTexture;
        Rect m_InGamePreviewRect;
        Texture2D m_InGamePreviewTexture2D;
        IVisualElementScheduledItem m_InGamePreviewScheduledItem;
        Camera backgroundCamera
        {
            get
            {
                var fieldValue = m_CameraField.value;
#if UNITY_2019_3_OR_NEWER
                var camera = fieldValue as Camera;
#else
                var camera = (fieldValue as GameObject)?.GetComponent<Camera>();
#endif
                return camera;
            }
        }

        // Background Control Containers
        VisualElement m_BackgroundColorModeControls;
        VisualElement m_BackgroundImageModeControls;
        VisualElement m_BackgroundCameraModeControls;

        public BuilderInspectorCanvas(BuilderInspector inspector)
        {
            m_Inspector = inspector;
            m_Document = inspector.document;
            m_CanvasInspector = m_Inspector.Q("canvas-inspector");

            var builderWindow = inspector.paneWindow as Builder;
            if (builderWindow == null)
                return;

            m_Canvas = builderWindow.canvas;

            m_CameraModeEnabled = false;

            // Size Fields
            m_CanvasWidth = root.Q<IntegerField>("canvas-width");
            m_CanvasWidth.isDelayed = true;
            m_CanvasWidth.RegisterValueChangedCallback(OnWidthChange);
            m_CanvasHeight = root.Q<IntegerField>("canvas-height");
            m_CanvasHeight.isDelayed = true;
            m_CanvasHeight.RegisterValueChangedCallback(OnHeightChange);
            m_Canvas.RegisterCallback<GeometryChangedEvent>(OnCanvasSizeChange);

            // Background Opacity
            m_OpacityField = root.Q<PercentSlider>("background-opacity-field");
            m_OpacityField.RegisterValueChangedCallback(OnBackgroundOpacityChange);

            // Setup Background Mode
            var backgroundModeType = typeof(BuilderCanvasBackgroundMode);
            var backgroundModeValues = Enum.GetValues(backgroundModeType)
                .OfType<BuilderCanvasBackgroundMode>().Select((v) => v.ToString()).ToList();
            var backgroundModeNames = Enum.GetNames(backgroundModeType);
            backgroundModeNames[0] = "Transparent";
            m_BackgroundMode = root.Q<ToggleButtonStrip>("background-mode-field");
            m_BackgroundMode.enumType = backgroundModeType;
            m_BackgroundMode.labels = backgroundModeNames;
            m_BackgroundMode.choices = backgroundModeValues;
            m_BackgroundMode.RegisterValueChangedCallback(OnBackgroundModeChange);

            // Color field.
            m_ColorField = root.Q<ColorField>("background-color-field");
            m_ColorField.RegisterValueChangedCallback(OnBackgroundColorChange);

            // Set Image field.
            m_ImageField = root.Q<ObjectField>("background-image-field");
            m_ImageField.objectType = typeof(Texture2D);
            m_ImageField.RegisterValueChangedCallback(OnBackgroundImageChange);
            m_ImageScaleModeField = root.Q<ToggleButtonStrip>("background-image-scale-mode-field");
            m_ImageScaleModeField.enumType = typeof(ScaleMode);
            var backgroundScaleModeValues = Enum.GetValues(typeof(ScaleMode))
                .OfType<ScaleMode>().Select((v) => BuilderNameUtilities.ConvertCamelToDash(v.ToString())).ToList();
            m_ImageScaleModeField.choices = backgroundScaleModeValues;
            m_ImageScaleModeField.RegisterValueChangedCallback(OnBackgroundImageScaleModeChange);
            m_FitCanvasToImageButton = root.Q<Button>("background-image-fit-canvas-button");
            m_FitCanvasToImageButton.clickable.clicked += FitCanvasToImage;

            // Set Camera field.
            m_CameraField = root.Q<ObjectField>("background-camera-field");
            m_CameraField.objectType = typeof(Camera);
            m_CameraField.RegisterValueChangedCallback(OnBackgroundCameraChange);

            // Control Containers
            m_BackgroundColorModeControls = root.Q("canvas-background-color-mode-controls");
            m_BackgroundImageModeControls = root.Q("canvas-background-image-mode-controls");
            m_BackgroundCameraModeControls = root.Q("canvas-background-camera-mode-controls");

            EditorApplication.playModeStateChanged += PlayModeStateChange;
        }

        public void Disable()
        {
            throw new NotImplementedException();
        }

        public void Enable()
        {
            throw new NotImplementedException();
        }

        public void Refresh()
        {
            // HACK until fix goes in:
            m_CanvasWidth.isDelayed = false;
            m_CanvasHeight.isDelayed = false;

            m_CanvasWidth.SetValueWithoutNotify(settings.CanvasWidth);
            m_CanvasHeight.SetValueWithoutNotify(settings.CanvasHeight);

            m_CanvasWidth.isDelayed = true;
            m_CanvasHeight.isDelayed = true;

            m_OpacityField.SetValueWithoutNotify(settings.CanvasBackgroundOpacity);
            m_BackgroundMode.SetValueWithoutNotify(settings.CanvasBackgroundMode.ToString());

            m_ColorField.SetValueWithoutNotify(settings.CanvasBackgroundColor);

            var scaleModeStr = settings.CanvasBackgroundImageScaleMode.ToString();
            scaleModeStr = BuilderNameUtilities.ConvertCamelToDash(scaleModeStr);
            m_ImageField.SetValueWithoutNotify(settings.CanvasBackgroundImage);
            m_ImageScaleModeField.SetValueWithoutNotify(scaleModeStr);

            m_CameraField.SetValueWithoutNotify(FindCameraByName());

            ApplyBackgroundOptions();
        }

        Camera FindCameraByName()
        {
            var cameraName = settings.CanvasBackgroundCameraName;
            if (string.IsNullOrEmpty(cameraName))
                return null;

            var camera = Camera.allCameras.FirstOrDefault((c) => c.name == cameraName);
            return camera;
        }

        void PostSettingsChange()
        {
            m_Document.SaveSettingsToDisk();
        }

        void ApplyBackgroundOptions()
        {
            DeactivateCameraMode();

            customBackgroundElement.style.backgroundColor = StyleKeyword.Null;
            customBackgroundElement.style.backgroundImage = StyleKeyword.Null;
            customBackgroundElement.style.unityBackgroundScaleMode = StyleKeyword.Null;

            m_BackgroundColorModeControls.AddToClassList(BuilderConstants.HiddenStyleClassName);
            m_BackgroundImageModeControls.AddToClassList(BuilderConstants.HiddenStyleClassName);
            m_BackgroundCameraModeControls.AddToClassList(BuilderConstants.HiddenStyleClassName);

            switch (settings.CanvasBackgroundMode)
            {
                case BuilderCanvasBackgroundMode.None: break;
                case BuilderCanvasBackgroundMode.Color:
                    customBackgroundElement.style.backgroundColor = settings.CanvasBackgroundColor;
                    m_BackgroundColorModeControls.RemoveFromClassList(BuilderConstants.HiddenStyleClassName);
                    break;
                case BuilderCanvasBackgroundMode.Image:
                    customBackgroundElement.style.backgroundImage = settings.CanvasBackgroundImage;
                    customBackgroundElement.style.unityBackgroundScaleMode = settings.CanvasBackgroundImageScaleMode;
                    m_BackgroundImageModeControls.RemoveFromClassList(BuilderConstants.HiddenStyleClassName);
                    break;
                case BuilderCanvasBackgroundMode.Camera:
                    ActivateCameraMode();
                    m_BackgroundCameraModeControls.RemoveFromClassList(BuilderConstants.HiddenStyleClassName);
                    break;
            }

            customBackgroundElement.style.opacity = settings.CanvasBackgroundOpacity;
        }

        void ActivateCameraMode()
        {
            if (m_CameraModeEnabled || backgroundCamera == null)
                return;

            UpdateCameraRects();

            m_InGamePreviewScheduledItem = customBackgroundElement.schedule.Execute(UpdateInGameBackground);
            m_InGamePreviewScheduledItem.Every(s_CameraRefreshDelayMS);

            m_CameraModeEnabled = true;
        }

        void DeactivateCameraMode()
        {
            if (!m_CameraModeEnabled)
                return;

            m_InGamePreviewScheduledItem.Pause();
            m_InGamePreviewScheduledItem = null;

            m_InGamePreviewRenderTexture = null;
            m_InGamePreviewTexture2D = null;

            m_CameraModeEnabled = false;

            customBackgroundElement.style.backgroundImage = StyleKeyword.Null;
        }

        void PlayModeStateChange(PlayModeStateChange state)
        {
            UpdateCameraRects();
        }

        void UpdateCameraRects()
        {
            if (settings.CanvasBackgroundMode != BuilderCanvasBackgroundMode.Camera)
                return;

            int width = 2 * settings.CanvasWidth;
            int height = 2 * settings.CanvasHeight;

            m_InGamePreviewRenderTexture = new RenderTexture(width, height, 1);
            m_InGamePreviewRect = new Rect(0, 0, width, height);
            m_InGamePreviewTexture2D = new Texture2D(width, height);
        }

        void UpdateInGameBackground()
        {
            if (backgroundCamera == null)
            {
                var refCamera = FindCameraByName();
                m_CameraField.value = null;
                m_CameraField.value = refCamera;
                return;
            }

            backgroundCamera.targetTexture = m_InGamePreviewRenderTexture;

            RenderTexture.active = m_InGamePreviewRenderTexture;
            backgroundCamera.Render();

            m_InGamePreviewTexture2D.ReadPixels(m_InGamePreviewRect, 0, 0);
            m_InGamePreviewTexture2D.Apply(false);

            RenderTexture.active = null;
            backgroundCamera.targetTexture = null;

            customBackgroundElement.style.backgroundImage = m_InGamePreviewTexture2D;
            customBackgroundElement.IncrementVersion(VersionChangeType.Repaint);
        }

        void OnCanvasSizeChange(GeometryChangedEvent evt)
        {
            // HACK until fix goes in:
            m_CanvasWidth.isDelayed = false;
            m_CanvasHeight.isDelayed = false;

            m_CanvasWidth.SetValueWithoutNotify((int)m_Canvas.width);
            m_CanvasHeight.SetValueWithoutNotify((int)m_Canvas.height);

            m_CanvasWidth.isDelayed = true;
            m_CanvasHeight.isDelayed = true;

            UpdateCameraRects();
        }

        void OnWidthChange(ChangeEvent<int> evt)
        {
            var newValue = evt.newValue;
            if (newValue < (int)BuilderConstants.CanvasMinWidth)
            {
                newValue = (int)BuilderConstants.CanvasMinWidth;
                var field = evt.target as IntegerField;

                // HACK until fix goes in:
                field.isDelayed = false;
                field.SetValueWithoutNotify(newValue);
                field.isDelayed = true;
            }

            settings.CanvasWidth = newValue;
            m_Canvas.width = newValue;
            UpdateCameraRects();
            PostSettingsChange();
        }

        void OnHeightChange(ChangeEvent<int> evt)
        {
            var newValue = evt.newValue;
            if (newValue < (int)BuilderConstants.CanvasMinHeight)
            {
                newValue = (int)BuilderConstants.CanvasMinHeight;

                var field = evt.target as IntegerField;

                // HACK until fix goes in:
                field.isDelayed = false;
                field.SetValueWithoutNotify(newValue);
                field.isDelayed = true;
            }

            settings.CanvasHeight = newValue;
            m_Canvas.height = newValue;
            UpdateCameraRects();
            PostSettingsChange();
        }

        void OnBackgroundOpacityChange(ChangeEvent<float> evt)
        {
            settings.CanvasBackgroundOpacity = evt.newValue;
            customBackgroundElement.style.opacity = evt.newValue;
            PostSettingsChange();
        }

        void OnBackgroundModeChange(ChangeEvent<string> evt)
        {
            var enumValue = (BuilderCanvasBackgroundMode) Enum.Parse(typeof(BuilderCanvasBackgroundMode), evt.newValue);
            settings.CanvasBackgroundMode = enumValue;
            PostSettingsChange();
            ApplyBackgroundOptions();
        }

        void OnBackgroundColorChange(ChangeEvent<Color> evt)
        {
            settings.CanvasBackgroundColor = evt.newValue;
            PostSettingsChange();

            if (settings.CanvasBackgroundMode != BuilderCanvasBackgroundMode.Color)
                return;

            customBackgroundElement.style.backgroundColor = evt.newValue;
        }

        void OnBackgroundImageChange(ChangeEvent<Object> evt)
        {
            settings.CanvasBackgroundImage = evt.newValue as Texture2D;
            PostSettingsChange();

            if (settings.CanvasBackgroundMode != BuilderCanvasBackgroundMode.Image)
                return;

            if (settings.CanvasBackgroundImage == null)
                customBackgroundElement.style.backgroundImage = StyleKeyword.Null;
            else
                customBackgroundElement.style.backgroundImage = settings.CanvasBackgroundImage;
        }

        void OnBackgroundImageScaleModeChange(ChangeEvent<string> evt)
        {
            var newValue = BuilderNameUtilities.ConvertDashToHungarian(evt.newValue);
            var enumValue = (ScaleMode)Enum.Parse(typeof(ScaleMode), newValue);
            settings.CanvasBackgroundImageScaleMode = enumValue;
            PostSettingsChange();

            if (settings.CanvasBackgroundMode != BuilderCanvasBackgroundMode.Image)
                return;

            customBackgroundElement.style.unityBackgroundScaleMode = enumValue;
        }

        void FitCanvasToImage()
        {
            if (settings.CanvasBackgroundImage == null)
                return;

            m_Canvas.width = settings.CanvasBackgroundImage.width;
            m_Canvas.height = settings.CanvasBackgroundImage.height;
        }

        void OnBackgroundCameraChange(ChangeEvent<Object> evt)
        {
            var previousCamera = evt.previousValue as Camera;
            if (Object.ReferenceEquals(previousCamera, evt.newValue))
                return;

#if UNITY_2019_3_OR_NEWER
            var camera = evt.newValue as Camera;
#else
            var camera = (evt.newValue as GameObject)?.GetComponent<Camera>();
#endif
            if (camera == null)
            {
                settings.CanvasBackgroundCameraName = null;
                DeactivateCameraMode();
            }
            else
            {
                settings.CanvasBackgroundCameraName = camera.name;
                ActivateCameraMode();
            }
            PostSettingsChange();
        }
    }
}
