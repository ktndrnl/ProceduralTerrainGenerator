using System;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

public class MenuEvents : MonoBehaviour
{
    public CameraController cameraController;
    
    private UIDocument _document;
    private VisualElement _menuContainer;
    
    // noise settings
    private IntegerField _seed;
    private IntegerField _scale;
    private Slider _persistence;
    private IntegerField _lacunarity;
    private Vector2Field _offset;
    private DropdownField _normalizeMode;
    
    // mesh settings
    private IntegerField _meshScale;
    private Toggle _useFlatShading;
    private SliderInt _chunkSizeIndex;
    private SliderInt _chunkSizeIndexFlatShaded;

    private Button _updateButton;

    private Label _menuHint;
    
    private bool _isMenuEnabled = true;

    private TerrainGenerator _terrainGenerator;

    private void Awake()
    {
        _document = GetComponent<UIDocument>();
        _menuContainer = _document.rootVisualElement.Q("MenuContainer");
        
        _seed = _document.rootVisualElement.Q<IntegerField>("Seed");
        // _seed.SetBinding("value", new DataBinding() 
        // {
        //     dataSourcePath = new PropertyPath(nameof(_terrainGenerator.heightMapSettings.noiseSettings.seed)),
        //     bindingMode = BindingMode.TwoWay
        // });
        
        _scale = _document.rootVisualElement.Q<IntegerField>("Scale");
        // _scale.SetBinding("value", new DataBinding() 
        // {
        //     dataSourcePath = new PropertyPath(nameof(_terrainGenerator.heightMapSettings.noiseSettings.scale)),
        //     bindingMode = BindingMode.TwoWay
        // });
        
        _persistence = _document.rootVisualElement.Q<Slider>("Persistence");
        // _persistence.SetBinding("value", new DataBinding() 
        // {
        //     dataSourcePath = new PropertyPath(nameof(_terrainGenerator.heightMapSettings.noiseSettings.persistence)),
        //     bindingMode = BindingMode.TwoWay
        // });
        
        _lacunarity = _document.rootVisualElement.Q<IntegerField>("Lacunarity");
        // _lacunarity.SetBinding("value", new DataBinding() 
        // {
        //     dataSourcePath = new PropertyPath(nameof(_terrainGenerator.heightMapSettings.noiseSettings.lacunarity)),
        //     bindingMode = BindingMode.TwoWay
        // });
        
        _offset = _document.rootVisualElement.Q<Vector2Field>("Offset");
        // _offset.SetBinding("value", new DataBinding() 
        // {
        //     dataSourcePath = new PropertyPath(nameof(_terrainGenerator.heightMapSettings.noiseSettings.offset)),
        //     bindingMode = BindingMode.TwoWay
        // });
        
        _normalizeMode = _document.rootVisualElement.Q<DropdownField>("NormalizeMode");
        // _normalizeMode.SetBinding("value", new DataBinding() 
        // {
        //     dataSourcePath = new PropertyPath(nameof(_terrainGenerator.heightMapSettings.noiseSettings.normalizeMode)),
        //     bindingMode = BindingMode.TwoWay
        // });
        
        _meshScale = _document.rootVisualElement.Q<IntegerField>("MeshScale");
        // _meshScale.SetBinding("value", new DataBinding() 
        // {
        //     dataSourcePath = new PropertyPath(nameof(_terrainGenerator.meshSettings.meshScale)),
        //     bindingMode = BindingMode.TwoWay
        // });
        
        _useFlatShading = _document.rootVisualElement.Q<Toggle>("UseFlatShading");
        // _useFlatShading.SetBinding("value", new DataBinding() 
        // {
        //     dataSourcePath = new PropertyPath(nameof(_terrainGenerator.meshSettings.useFlatShading)),
        //     bindingMode = BindingMode.TwoWay
        // });
        
        _chunkSizeIndex = _document.rootVisualElement.Q<SliderInt>("ChunkSizeIndex");
        // _chunkSizeIndex.SetBinding("value", new DataBinding() 
        // {
        //     dataSourcePath = new PropertyPath(nameof(_terrainGenerator.meshSettings.chunkSizeIndex)),
        //     bindingMode = BindingMode.TwoWay
        // });
        
        _chunkSizeIndexFlatShaded = _document.rootVisualElement.Q<SliderInt>("ChunkSizeIndexFlatShaded");
        // _chunkSizeIndexFlatShaded.SetBinding("value", new DataBinding() 
        // {
        //     dataSourcePath = new PropertyPath(nameof(_terrainGenerator.meshSettings.flatShadedChunkSizeIndex)),
        //     bindingMode = BindingMode.TwoWay
        // });
        
        _updateButton = _document.rootVisualElement.Q<Button>("UpdateButton");
        
        _menuHint = _document.rootVisualElement.Q<Label>("MenuHint");

        _terrainGenerator = FindFirstObjectByType<TerrainGenerator>();
    }

    private void Start()
    {
        cameraController.OnCameraMove += OnCameraMove;
        _updateButton.clicked += OnUpdateButtonClicked;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_isMenuEnabled)
            {
                DisableMenu();
            }
            else
            {
                EnableMenu();
            }
        }
    }

    private void OnUpdateButtonClicked()
    {
        _terrainGenerator.ResetTerrain();
    }

    private void OnDisable()
    {
        cameraController.OnCameraMove -= OnCameraMove;
    }

    private void OnCameraMove()
    {
        DisableMenu();
    }
    
    private void DisableMenu()
    {
        _menuContainer.SetEnabled(false);
        _menuHint.SetEnabled(true);
        _isMenuEnabled = false;
    }
    
    private void EnableMenu()
    {
        _menuContainer.SetEnabled(true);
        _menuHint.SetEnabled(false);
        _isMenuEnabled = true;
    }
}
