using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ComputeShaderRenderer : MonoBehaviour
{
    public ComputeShader computeShader; 
    private RenderTexture renderTexture = null;
    [SerializeField] private int xResolution = 256;
    [SerializeField] private int yResolution = 256;
    [SerializeField] private bool overrideResolution = false;
    
    private ComputeBuffer _accumBuffer;

    public CustomCamera _camera;//new CustomCamera(new Vector3(0, 0, -3),new Vector3(0,0,1), 67);
    [SerializeField] private float rotationSpeed = 1.0f;

    int kernelHandle;
    
    [Header("Render settings")]
    [SerializeField] private Vector3 diffuse = new Vector3(0.9f, 0.4f, 0.4f);
    [SerializeField] private float roughness = 0.5f;
    [SerializeField] private float specular = 0.5f; // 50% specular reflection
    [SerializeField] private float subsurface = 0.5f; // 50% subsurface scattering
    [SerializeField] private float _subsurfIterations = 4.0f;
    [SerializeField] private float _maxDistance = 50.0f;
    [SerializeField] private float _maxSteps = 100.0f;

    [Header("Debug")]
    [SerializeField] private bool _debugShowNormals = false;
    void Start()
    {
        if (overrideResolution)
        {
            var res = new Vector2Int(xResolution, (int)(yResolution * ((float)Screen.height / (float)Screen.width)));
            xResolution = res.x;
            yResolution = res.y;
        }
        else
        {
            xResolution = Screen.width;
            yResolution = Screen.height;
        }
        RenderPipelineManager.endContextRendering += OnEndContextRendering;
        gameObject.GetComponent<CameraFovChanger>().camera = _camera;

        _accumBuffer = new ComputeBuffer(xResolution * yResolution, sizeof(float) * 3);
        _accumBuffer.SetData(new float[xResolution * yResolution * 3]);
        computeShader.SetBuffer(kernelHandle, "_accumBuffer", _accumBuffer);
    }

    private float _time;
    private int _frameCount;
    private bool _saveFrame = false;
    void Update()
    {
        HandleMovement();

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetAccumulation();
        }
        
        if (Input.GetKeyDown(KeyCode.F2))
        {
            _saveFrame = true;
        }

        CheckScreenSize();
        
        _time += Time.deltaTime;
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(xResolution, yResolution, 24);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();
        }

        RenderFrame();

        if (_saveFrame)
        {
            _saveFrame = false;
            bool oldDebug = _debugShowNormals;
            _debugShowNormals = false;

            RenderFrame();
                SaveTextureToFileUtility.SaveRenderTextureToFile(renderTexture, "out.png", SaveTextureToFileUtility.SaveTextureFileFormat.PNG);
            
            _debugShowNormals = oldDebug;
        }

    }

    private void RenderFrame()
    {
        kernelHandle = computeShader.FindKernel("CSMain");
        computeShader.SetTexture(kernelHandle, "Result", renderTexture);
        computeShader.SetInt("_xResolution", xResolution);
        computeShader.SetInt("_yResolution", yResolution);

        computeShader.SetVector("_cameraPosition", _camera.position);
        computeShader.SetVector("_cameraForward", _camera.forward);
        computeShader.SetVector("_cameraUp", _camera.up);
        computeShader.SetVector("_cameraRight", _camera.right);
        computeShader.SetFloat("_cameraFov", MathHelper.Deg2Rad(_camera.fieldOfView));
        computeShader.SetFloat("_cameraAspect", (float)yResolution / xResolution);

        computeShader.SetFloat("_epsilon", 0.003f);
        computeShader.SetFloat("_maxDistance", _maxDistance);
        computeShader.SetFloat("_maxSteps", _maxSteps);
        
        computeShader.SetFloat("_roughness", roughness);
        computeShader.SetFloat("_specular", specular);
        computeShader.SetVector("diffuse", diffuse);
        computeShader.SetFloat("_subsurfStrength", subsurface);
        computeShader.SetFloat("_subsurfIterations", _subsurfIterations);

        computeShader.SetFloat("_time", _time);

        _frameCount++;
        computeShader.SetFloat("_frame", _frameCount);
        
        computeShader.SetBool("_debugShowNormals", _debugShowNormals);
        
        computeShader.Dispatch(kernelHandle, xResolution / 16, yResolution / 16, 1);
    }

    private void CheckScreenSize()
    {
        if (xResolution != Screen.width || yResolution != Screen.height)
        {
            if (overrideResolution) return;
            xResolution = Screen.width;
            yResolution = Screen.height;
            renderTexture = new RenderTexture(xResolution, yResolution, 24);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();

            _accumBuffer.Release();
            _accumBuffer = new ComputeBuffer(xResolution * yResolution, sizeof(float) * 3);
            _accumBuffer.SetData(new float[xResolution * yResolution * 3]);
            computeShader.SetBuffer(kernelHandle, "_accumBuffer", _accumBuffer);

            ResetAccumulation();
        }
    }

    public void ResetAccumulation()
    {
        //_accumBuffer.SetData(new float[xResolution * yResolution * 3]);
        _frameCount = 0;
    }

    private bool lockMouse = false;
    
    private void HandleMovement()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            lockMouse = !lockMouse;
            Cursor.lockState = lockMouse ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !lockMouse;
        }

        if (lockMouse)
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            if (horizontal != 0 || vertical != 0)
            {
                ResetAccumulation();
            }

            _camera.position -= _camera.right * horizontal * Time.deltaTime;
            _camera.position += _camera.forward * vertical * Time.deltaTime;

            if (Input.GetKey(KeyCode.Space))
            {
                _camera.position -= _camera.up * Time.deltaTime;
                ResetAccumulation();
            }

            if (Input.GetKey(KeyCode.LeftShift))
            {
                _camera.position += _camera.up * Time.deltaTime;
                ResetAccumulation();
            }

            MouseMovement();
        }
    }

    private void MouseMovement()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        mouseX += Input.GetAxis("J2Horizontal");
        mouseY += Input.GetAxis("J2Vertical");

        if (mouseX != 0 || mouseY != 0)
        {
            ResetAccumulation();
        }

        _camera.Rotate(Vector3.up, -mouseX * rotationSpeed);
        _camera.Rotate(_camera.right, mouseY * rotationSpeed);
    }

    void OnEndContextRendering(ScriptableRenderContext context, List<Camera> cameras)
    {
        if (!_saveFrame)
        {
            // Now to blit the render texture to the screen
            Graphics.Blit(renderTexture, (RenderTexture)null);
        }
    }

    void OnDestroy()
    {
        _accumBuffer.Release();
    }
}
