// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.IO;

#if MONOMAC
using MonoMac.OpenGL;
#elif WINDOWS || LINUX
using OpenTK.Graphics.OpenGL;
#elif GLES
using System.Text;
using OpenTK.Graphics.ES20;
using ShaderType = OpenTK.Graphics.ES20.All;
using ShaderParameter = OpenTK.Graphics.ES20.All;
using TextureUnit = OpenTK.Graphics.ES20.All;
using TextureTarget = OpenTK.Graphics.ES20.All;
#endif

namespace Microsoft.Xna.Framework.Graphics
{
    internal partial class Shader
    {
        // The shader handle.
        private int _shaderHandle = -1;

        // We keep this around for recompiling on context lost and debugging.
        private string _glslCode;

        private struct Attribute
        {
            public VertexElementUsage usage;
            public int index;
            public string name;
            public short format;
            public int location;
        }

        private Attribute[] _attributes;

        private void PlatformConstruct(BinaryReader reader, bool isVertexShader, byte[] shaderBytecode)
        {
            _glslCode = System.Text.Encoding.ASCII.GetString(shaderBytecode);

            HashKey = MonoGame.Utilities.Hash.ComputeHash(shaderBytecode);

            var attributeCount = (int)reader.ReadByte();
            _attributes = new Attribute[attributeCount];
            for (var a = 0; a < attributeCount; a++)
            {
                _attributes[a].name = reader.ReadString();
                _attributes[a].usage = (VertexElementUsage)reader.ReadByte();
                _attributes[a].index = reader.ReadByte();
                _attributes[a].format = reader.ReadInt16();
            }
        }

        internal int GetShaderHandle()
        {
            // If the shader has already been created then return it.
            if (_shaderHandle != -1)
                return _shaderHandle;
            
            //
            _shaderHandle = GL.CreateShader(Stage == ShaderStage.Vertex ? ShaderType.VertexShader : ShaderType.FragmentShader);
            GraphicsExtensions.CheckGLError();
#if GLES
			GL.ShaderSource(_shaderHandle, 1, new string[] { _glslCode }, (int[])null);
#else
            GL.ShaderSource(_shaderHandle, _glslCode);
#endif
            GraphicsExtensions.CheckGLError();
            GL.CompileShader(_shaderHandle);
            GraphicsExtensions.CheckGLError();

            var compiled = 0;
#if GLES
			GL.GetShader(_shaderHandle, ShaderParameter.CompileStatus, ref compiled);
#else
            GL.GetShader(_shaderHandle, ShaderParameter.CompileStatus, out compiled);
#endif
            GraphicsExtensions.CheckGLError();
            if (compiled == (int)All.False)
            {
#if GLES
                string log = "";
                int length = 0;
				GL.GetShader(_shaderHandle, ShaderParameter.InfoLogLength, ref length);
                GraphicsExtensions.CheckGLError();
                if (length > 0)
                {
                    var logBuilder = new StringBuilder(length);
					GL.GetShaderInfoLog(_shaderHandle, length, ref length, logBuilder);
                    GraphicsExtensions.CheckGLError();
                    log = logBuilder.ToString();
                }
#else
                var log = GL.GetShaderInfoLog(_shaderHandle);
#endif
                Console.WriteLine(log);

                if (GL.IsShader(_shaderHandle))
                {
                    GL.DeleteShader(_shaderHandle);
                    GraphicsExtensions.CheckGLError();
                }
                _shaderHandle = -1;

                throw new InvalidOperationException("Shader Compilation Failed");
            }

            return _shaderHandle;
        }

        internal void GetVertexAttributeLocations(int program)
        {
            for (int i = 0; i < _attributes.Length; ++i)
            {
                _attributes[i].location = GL.GetAttribLocation(program, _attributes[i].name);
                GraphicsExtensions.CheckGLError();
            }
        }

        internal int GetAttribLocation(VertexElementUsage usage, int index)
        {
            for (int i = 0; i < _attributes.Length; ++i)
            {
                if ((_attributes[i].usage == usage) && (_attributes[i].index == index))
                    return _attributes[i].location;
            }
            return -1;
        }

        internal void ApplySamplerTextureUnits(int program)
        {
            // Assign the texture unit index to the sampler uniforms.
            foreach (var sampler in Samplers)
            {
                var loc = GL.GetUniformLocation(program, sampler.name);
                GraphicsExtensions.CheckGLError();
                if (loc != -1)
                {
                    GL.Uniform1(loc, sampler.textureSlot);
                    GraphicsExtensions.CheckGLError();
                }
            }
        }

        private void PlatformGraphicsDeviceResetting()
        {
            if (_shaderHandle != -1)
            {
                if (GL.IsShader(_shaderHandle))
                {
                    GL.DeleteShader(_shaderHandle);
                    GraphicsExtensions.CheckGLError();
                }
                _shaderHandle = -1;
            }
        }

        private void PlatformDispose()
        {
            GraphicsDevice.AddDisposeAction(() =>
            {
                if (_shaderHandle != -1)
                {
                    if (GL.IsShader(_shaderHandle))
                    {
                        GL.DeleteShader(_shaderHandle);
                        GraphicsExtensions.CheckGLError();
                    }
                    _shaderHandle = -1;
                }
            });
        }
    }
}
