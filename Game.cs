using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Windows;
using SharpDX.DXGI;
using D3D11 = SharpDX.Direct3D11;
using SharpDX.Direct3D;
using SharpDX.D3DCompiler;
using SharpDX;
using SharpDX.Direct3D11;

namespace DirectXTest
{
    public class Game : IDisposable
    {
        private RenderForm _renderForm;
        private const int Width = 1000;
        private const int Height = 1000;

        private D3D11.Device _d3dDevice;
        private D3D11.DeviceContext _d3dDeviceContext;
        private SwapChain _swapChain;

        private D3D11.RenderTargetView _renderTargetView;
        private D3D11.Buffer _triangleVertexBuffer;

        private D3D11.VertexShader _vertexShader;
        private D3D11.PixelShader _pixelShader;

        private Vector3[][] _squares;

        private ShaderSignature _inputSignature;
        private D3D11.InputLayout _inputLayout;

        private Viewport _viewPort;

        private Random _random = new Random();

        private D3D11.InputElement[] _inputElements = new D3D11.InputElement[]
        {
            new D3D11.InputElement("POSITION", 0, Format.R32G32B32_Float, 0)
        };

        public Game()
        {
            _renderForm = new RenderForm("My first SharpDX game");
            _renderForm.ClientSize = new System.Drawing.Size(Width, Height);
            _renderForm.AllowUserResizing = false;

            InitializeDeviceResources();
            InitializeShaders();
            InitializeSquares();
        }

        public void Run()
        {
            RenderLoop.Run(_renderForm, RenderCallback);
        }

        private void RenderCallback()
        {
            Draw();
        }

        private Vector3[][] CreateRandomSquares(int numberOfTriangles)
        {

            Vector3[][] toReturn = new Vector3[numberOfTriangles][];

            for (int i = 0; i < numberOfTriangles; i++)
            {
                var toAdd = new Vector3[4];

                Vector3 random = new Vector3
                {
                    X = _random.NextFloat(0f, 2f),
                    Y = _random.NextFloat(0f, 2f),
                    Z = _random.NextFloat(-1f, 1f)
                };

                var bl = ApplyTransform(new Vector3(-1f, -1f, 0f), random);
                var ul = ApplyTransform(new Vector3(-1f, -0.75f, 0f), random);
                var ur = ApplyTransform(new Vector3(-0.75f, -0.75f, 0f), random);
                var br = ApplyTransform(new Vector3(-0.75f, -1f, 0f), random);

                toAdd[0] = ul;
                toAdd[1] = ur;
                toAdd[2] = bl;
                toAdd[3] = br;

                Console.WriteLine(ul);

                toReturn[i] = toAdd;
            }

            return toReturn;
        }

        private Vector3 ApplyTransform(Vector3 to, Vector3 transform)
        {
            return new Vector3
            {
                X = to.X + transform.X,
                Y = to.Y + transform.Y //,
                                       // Z = to.Z + transform.Z
            };
        }

        private void InitializeDeviceResources()
        {
            ModeDescription backBufferDesc = new ModeDescription(Width, Height, new Rational(60, 1), Format.R8G8B8A8_UNorm);

            SwapChainDescription swapChainDesc = new SwapChainDescription()
            {
                ModeDescription = backBufferDesc,
                SampleDescription = new SampleDescription(1, 0),
                Usage = Usage.RenderTargetOutput,
                BufferCount = 1,
                OutputHandle = _renderForm.Handle,
                IsWindowed = true
            };

            D3D11.Device.CreateWithSwapChain(DriverType.Hardware, D3D11.DeviceCreationFlags.None, swapChainDesc, out _d3dDevice, out _swapChain);
            _d3dDeviceContext = _d3dDevice.ImmediateContext;

            using (D3D11.Texture2D backBuffer = _swapChain.GetBackBuffer<D3D11.Texture2D>(0))
            {
                _renderTargetView = new D3D11.RenderTargetView(_d3dDevice, backBuffer);
            }

            _viewPort = new Viewport(0, 0, Width, Height);
            _d3dDeviceContext.Rasterizer.SetViewport(_viewPort);
        }

        private void InitializeShaders()
        {
            using (var vertexShaderByteCode = ShaderBytecode.CompileFromFile("vertexShader.hlsl", "main", "vs_4_0", ShaderFlags.Debug))
            {
                _inputSignature = ShaderSignature.GetInputSignature(vertexShaderByteCode);
                _vertexShader = new D3D11.VertexShader(_d3dDevice, vertexShaderByteCode);
            }
            using (var pixelShaderByteCode = ShaderBytecode.CompileFromFile("pixelShader.hlsl", "main", "ps_4_0", ShaderFlags.Debug))
            {
                _pixelShader = new D3D11.PixelShader(_d3dDevice, pixelShaderByteCode);
            }

            _d3dDeviceContext.VertexShader.Set(_vertexShader);
            _d3dDeviceContext.PixelShader.Set(_pixelShader);
            _d3dDeviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;

            _inputLayout = new D3D11.InputLayout(_d3dDevice, _inputSignature, _inputElements);
            _d3dDeviceContext.InputAssembler.InputLayout = _inputLayout;
        }

        private void Draw()
        {
            _d3dDeviceContext.OutputMerger.SetRenderTargets(_renderTargetView);
            _d3dDeviceContext.ClearRenderTargetView(_renderTargetView, new SharpDX.Color(32, 103, 178));

            for(int squareIndex = 0; squareIndex < _squares.Length; squareIndex++)
            {
                var square = _squares[squareIndex];
                for (int vertexIndex = 0; vertexIndex < square.Length; vertexIndex++)
                {
                    square[vertexIndex].X += 0.001f;
                }
            }

            for (int squareIndex = 0; squareIndex < _squares.Count(); squareIndex++)
            {
                var square = _squares[squareIndex];
                var buffer = D3D11.Buffer.Create<Vector3>(_d3dDevice, D3D11.BindFlags.VertexBuffer, square);
                var bufferBinding = new D3D11.VertexBufferBinding(buffer, Utilities.SizeOf<Vector3>(), 0);

                _d3dDeviceContext.InputAssembler.SetVertexBuffers(0, bufferBinding);
                _d3dDeviceContext.Draw(4, 0);
            }

            _swapChain.Present(1, PresentFlags.None);
        }


        private void InitializeSquares()
        {
            _squares = CreateRandomSquares(4);
        }

        public void Dispose()
        {
            _inputLayout.Dispose();
            _inputSignature.Dispose();
            _vertexShader.Dispose();
            _pixelShader.Dispose();
            _renderTargetView.Dispose();
            _swapChain.Dispose();
            _d3dDevice.Dispose();
            _d3dDeviceContext.Dispose();
            _renderForm.Dispose();
        }
    }
}
