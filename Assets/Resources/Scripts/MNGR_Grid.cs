using UnityEngine;

public class MNGR_Grid : MonoBehaviour
{
    public float _Camera_ZoomSpeed;
    public Vector2 _Camera_ZoomRange;
    public float _Camera_MoveSpeed;

    [Header("NoiseMap controler")]
        public Color _BackGroundColor;
        public bool _DisplayNoiseMap;
        [Range(-150, 150)] public float _NoiseMapXPosition;
        [Range(-150, 150)] public float _NoiseMapZPosition;
        [Range(0, 100)] public float _NoiseMapSize;
        Transform _NoiseMap;
        

    [Header("Grid parameters")]
        public bool _GenerateGrid;
        [Range(1, 200)] public int _GridWidth;
        [Range(1, 200)] public int _GridHeight;
        GameObject _Model_Hex;
        Material _MAT_Hex;
    
    [Header("Map Coloration")]
        [Range(0,1)] public float _Value_WaterDeep;
        public Color _Color_WaterDeeps;
        [Range(0,1)] public float _Value_Water;
        public Color _Color_Water;
        [Range(0,1)] public float _Value_Sand;
        public Color _Color_Sand;
        [Range(0,1)] public float _Value_Meadow;
        public Color _Color_Meadow;
        [Range(0,1)] public float _Value_Forest;
        public Color _Color_Forest;
        [Range(0,1)] public float _Value_Mountains;
        public Color _Color_Moutains;
        [Range(0,1)] public float _Value_Lava;
        public Color _Color_Lava;
        [Range(0,1)] public float _Value_Volcano;
        public Color _Color_Volcano;

    

    void Start()
    {
        _NoiseMap = GameObject.Find("NoiseMap").transform;
        _Model_Hex = Resources.Load<GameObject>("Models/Hexagone");
        _MAT_Hex = Resources.Load<Material>("Materials/MAT_Hexagone");
        _GenerateGrid = true;
    }

    void Update()
    {
        Update_Camera();
        Update_NoiseMap();

        if (_GenerateGrid == true)
            Generate_HexGrid();

        Color_Hexagones();
    }

    /// <summary>
    /// Manage camera zoom with mousewheel
    /// </summary>
    void Update_Camera()
    {
        // Manage zoom
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0f)
        {
            float _PosY = Camera.main.transform.position.y - scrollInput * _Camera_ZoomSpeed;
            _PosY = Mathf.Clamp(_PosY, _Camera_ZoomRange.x, _Camera_ZoomRange.y);

            Camera.main.transform.position = new Vector3(Camera.main.transform.position.x, _PosY, Camera.main.transform.position.z);
        }

        // Manage movement
        Vector3 _MoveDirection = Vector3.zero;
        if (Input.GetKey(KeyCode.A))
            _MoveDirection += Vector3.left;
        if (Input.GetKey(KeyCode.D))
            _MoveDirection += Vector3.right;
        if (Input.GetKey(KeyCode.W))
            _MoveDirection += Vector3.forward;
        if (Input.GetKey(KeyCode.S))
            _MoveDirection += Vector3.back;

        if (_MoveDirection != Vector3.zero)
            Camera.main.transform.position += _MoveDirection * _Camera_MoveSpeed * Time.deltaTime;
    }

    /// <summary>
    /// Allow to modify noise map from grid editor
    /// </summary>
    void Update_NoiseMap()
    {
        Camera.main.backgroundColor = _BackGroundColor;

        _NoiseMap.position = new Vector3(_NoiseMapXPosition, -1, _NoiseMapZPosition);
        _NoiseMap.localScale = _NoiseMapSize * Vector3.one;
        _NoiseMap.GetComponent<Renderer>().enabled = _DisplayNoiseMap;
    }

    /// <summary>
    /// Allow to modify noise map from grid editor
    /// </summary>
    void Generate_HexGrid()
    {
        if (_Model_Hex == null)
        {
            Debug.LogError("Hexagone prefab not found in Resources");
            return;
        }

        for (int _ChildId = transform.childCount - 1; _ChildId >= 0; _ChildId--)
            Destroy(transform.GetChild(_ChildId).gameObject);

        for (int x = 0; x < _GridWidth; x++)
        {
            for (int z = 0; z < _GridHeight; z++)
            {
                float _XPos = x * 0.86f;
                float _ZPos = z * 3;
                if (x % 2 == 1)
                    _ZPos += 1.5f;

                // Instantiate hexagon at calculated position
                GameObject _Hex = Instantiate(_Model_Hex, new Vector3(_XPos, 0, _ZPos), Quaternion.identity);
                _Hex.transform.SetParent(transform);
                _Hex.GetComponent<Renderer>().material = _MAT_Hex;
            }
        }

        _GenerateGrid = false;
    }

    void Color_Hexagones()
    {
        foreach (Transform _Hexagone in transform)
        {
            Color _Color = Get_ColorAtWorldPosition(_Hexagone.position);
            _Hexagone.GetComponent<Renderer>().material.color = _Color;
        }
    }


    /// <summary>
    /// Get color of noise map at given world position
    /// Replace this color by a map color
    /// </summary>
    Color Get_ColorAtWorldPosition(Vector3 WorldPosition)
    {
        Color _Color = Color.magenta;

        // Check for a noise map at position
        // Noise map should be between YPosition [100,-100] or it will not be found
        Ray _Ray = new Ray(WorldPosition + 100 * Vector3.up, Vector3.down);
        RaycastHit[] _Hit = Physics.RaycastAll(_Ray, 200f);

        for (int _HitID = 0; _HitID < _Hit.Length; _HitID++)
        {
            // Ignore anything other than NoiseMap
            if (_Hit[_HitID].transform.tag != "NoiseMap")
                break;

            Renderer _Renderer = _Hit[_HitID].collider.GetComponent<Renderer>();
            if (_Renderer != null && _Renderer.material.HasProperty("_MainTex"))
            {
                Texture2D _Tex = _Renderer.material.GetTexture("_MainTex") as Texture2D;

                if (_Tex != null)
                {
                    // Convert world hit position to UV coordinates
                    Vector2 _UV = _Hit[_HitID].textureCoord;

                    // Convert UV to pixel coordinates
                    int _TexX = Mathf.FloorToInt(_UV.x * _Tex.width);
                    int _TexY = Mathf.FloorToInt(_UV.y * _Tex.height);

                    // Get noise pixel color
                    _Color = _Tex.GetPixel(_TexX, _TexY);

                    // Replace noise color into map color
                    _Color = Get_MapColor(_Color);
                }
                else
                    Debug.Log("No texture found on material.");
            }
        }

        return _Color;
    }

    /// <summary>
    /// Replace a noise color (gradient of white/grey/black) by a map color depending of brightness
    /// Each color is managed from the darkest to brightest one (change order if needed)
    /// If noise color brightness not in map color range => displayed as Magenta
    /// </summary>
    /// <param name="NoiseColor"></param>
    /// <returns></returns>

    Color Get_MapColor(Color NoiseColor)
    {
        Color _MapColor = Color.magenta;

        float _Brightness = NoiseColor.r; 
        if (_Brightness < _Value_WaterDeep)
            _MapColor = _Color_WaterDeeps;
        else if (_Brightness < _Value_Water)
            _MapColor = _Color_Water;
        else if (_Brightness < _Value_Water)
            _MapColor = _Color_Water;
        else if (_Brightness < _Value_Sand)
            _MapColor = _Color_Sand;
        else if (_Brightness < _Value_Meadow)
            _MapColor = _Color_Meadow;
        else if (_Brightness < _Value_Forest)
            _MapColor = _Color_Forest;
        else if (_Brightness < _Value_Mountains)
            _MapColor = _Color_Moutains;
        else if (_Brightness < _Value_Lava)
            _MapColor = _Color_Lava;
        else if (_Brightness < _Value_Volcano)
            _MapColor = _Color_Volcano;

        return _MapColor;
    }
}
