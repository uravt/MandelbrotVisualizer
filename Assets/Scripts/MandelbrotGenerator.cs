using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MandelbrotGenerator : MonoBehaviour
{
    public ComputeShader computeShader;
    public RenderTexture renderTexture;

    [SerializeField] public float boundXLower = -2.4f;
    [SerializeField] public float boundXUpper = 1f;
    [SerializeField] public float boundYLower = -1.12f;
    [SerializeField] public float boundYUpper = 1.12f;

    [SerializeField] public bool isPaused = false;
    
    [SerializeField] float scrollMultiplier = 0.1f;
    [SerializeField] float translateConstant = 0.01f;
    
    private int boundXLowerID;
    private int boundXUpperID;
    private int boundYLowerID;
    private int boundYUpperID;
    private int widthID;
    private int heightID;
    private int mousePosXID;
    private int mousePosYID;
    private int debugBufferID;
    
    public ComputeBuffer debugBuffer;
    private int[] debugData;

    public Gradient gradient;
    
    // Start is called before the first frame update
    private void Awake()
    {
        gradient = new Gradient();
        GradientColorKey[] colors = new GradientColorKey[8];
        colors[0] = new GradientColorKey(Color.red, 0.0f);
        colors[1] = new GradientColorKey(Color.blue, 1.0f);
        
        var alphas = new GradientAlphaKey[1];
        alphas[0] = new GradientAlphaKey(1.0f, 0.0f);
        
        gradient.SetKeys(colors, alphas);
    }

    void Start()
    {
        renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();
        
        computeShader.SetFloat("boundXLowerInit", boundXLower);
        computeShader.SetFloat("boundXUpperInit", boundXUpper);
        computeShader.SetFloat("boundYLowerInit", boundYLower);
        computeShader.SetFloat("boundYUpperInit", boundYUpper);
        
        boundXLowerID = Shader.PropertyToID("boundXLower");
        boundXUpperID = Shader.PropertyToID("boundXUpper");
        boundYLowerID = Shader.PropertyToID("boundYLower");
        boundYUpperID = Shader.PropertyToID("boundYUpper");
        widthID = Shader.PropertyToID("width");
        heightID = Shader.PropertyToID("height");

        mousePosXID = Shader.PropertyToID("mousePosX");
        mousePosYID = Shader.PropertyToID("mousePosY");
        
        debugBufferID = Shader.PropertyToID("debug_buffer");
        
        computeShader.SetFloat(widthID, Screen.width);
        computeShader.SetFloat(heightID, Screen.height);
        
        UpdateGPUData();
        
        computeShader.SetTexture(0, "Result", renderTexture);
        computeShader.SetTexture(1, "Result", renderTexture);
        
        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            computeShader.Dispatch(0, renderTexture.width/8,renderTexture.height/8, 1);
        }
        else
        {
            computeShader.Dispatch(1, renderTexture.width/8,renderTexture.height/8, 1);
        }

        debugBuffer = new ComputeBuffer(Screen.width * Screen.height, 4);
        debugData = new int[debugBuffer.count];
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();
        }
        
        computeShader.SetTexture(0, "Result", renderTexture);
        computeShader.SetTexture(1, "Result", renderTexture);
        UpdateGPUData();
        
        int groupsX = Mathf.CeilToInt(renderTexture.width / 8f);
        int groupsY = Mathf.CeilToInt(renderTexture.height / 8f);
        
        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            computeShader.Dispatch(0, groupsX,groupsY, 1);
        }
        else
        {
            computeShader.Dispatch(1, groupsX,groupsY, 1);
        }
        Graphics.Blit(renderTexture, destination);
    }

    private void UpdateGPUData()
    {
        computeShader.SetFloat(boundXLowerID, boundXLower);
        computeShader.SetFloat(boundXUpperID, boundXUpper);
        computeShader.SetFloat(boundYLowerID, boundYLower);
        computeShader.SetFloat(boundYUpperID, boundYUpper);
        if (!isPaused)
        {
            computeShader.SetFloat(mousePosXID, Input.mousePosition.x);
            computeShader.SetFloat(mousePosYID, Input.mousePosition.y);
        }
        
        computeShader.SetBuffer(computeShader.FindKernel("CSMain") , debugBufferID, debugBuffer);
    }

    private void Update()
    {
        Vector2 scroll = Input.mouseScrollDelta;
        Vector2 mousePos = Input.mousePosition;
        Vector2 midpoint = new Vector2(boundXLower + (boundXUpper - boundXLower) / 2,
            boundYLower + (boundYUpper - boundYLower));
        Vector2 lookPoint = new Vector2(boundXLower + (boundXUpper - boundXLower) * (mousePos.x / Screen.width),
            boundYLower + (boundYUpper - boundYLower) * (mousePos.y / Screen.height));
        midpoint = lookPoint;
        
        if (scroll.sqrMagnitude > 0)
        {
            boundXLower = midpoint.x - (midpoint.x - boundXLower) * (1 + scroll.y * scrollMultiplier); 
            boundXUpper = midpoint.x + (boundXUpper - midpoint.x) * (1 + scroll.y * scrollMultiplier); 
            boundYLower = midpoint.y - (midpoint.y - boundYLower) * (1 + scroll.y * scrollMultiplier); 
            boundYUpper = midpoint.y + (boundYUpper - midpoint.y) * (1 + scroll.y * scrollMultiplier); 
        }

        float xScrollConst = (boundXUpper - boundXLower) * translateConstant;
        float yScrollConst = (boundYUpper - boundYLower) * translateConstant;
        
        if (Input.GetKey(KeyCode.A))
        {
            boundXLower -= xScrollConst;
            boundXUpper -= xScrollConst;
        }
        if (Input.GetKey(KeyCode.D))
        {
            boundXLower += xScrollConst;
            boundXUpper += xScrollConst;
        }
        if (Input.GetKey(KeyCode.S))
        {
            boundYLower -= yScrollConst;
            boundYUpper -= yScrollConst;
        }
        if (Input.GetKey(KeyCode.W))
        {
            boundYLower += yScrollConst;
            boundYUpper += yScrollConst;
        }

        if(Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            SavePNG();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            isPaused = !isPaused;
        }
    }
    
    void OnEnable () {
        debugBuffer = new ComputeBuffer(Screen.width * Screen.height, 4);
    }
    
    void OnDisable () {
        debugBuffer.Release();
    }

    void SavePNG()
    { 
        Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
        // ReadPixels looks at the active RenderTexture.
        RenderTexture.active = renderTexture;
        tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        tex.Apply();
        byte[] bytes = tex.EncodeToPNG();
        var dirPath = Application.persistentDataPath + "/../SaveImages/";
        if(!Directory.Exists(dirPath)) {
            Directory.CreateDirectory(dirPath);
        }
        File.WriteAllBytes(dirPath + DateTime.Now.ToString("yyyyMMddTHHmmss") + ".png", bytes);
    }
}
