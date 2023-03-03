﻿using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.IO;
using System.Drawing;

using msg = System.Windows;   //vytvorím alias pre namespace, lebo 2 rozne namespacy maju clasu s rovnakym nazvom

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;


namespace DiplomovaPracaLB
{
    public partial class MainWindow : Window
    {
        GLControl glControl;

        // camera settings
        double Dist = new double(), Phi = new double(), Theta = new double(), oPhi = new double(), oTheta = new double();
        // mouse settings
        double RightX, RightY;
        bool IsRightDown;
        bool show_Axes, show_Points, show_Wireframe, show_Quads, point_color_gradients, quad_color_gradient;

        //color settings
        float[] FarebnaLegendaHodnoty;
        float[][] FarebnaLegendaFarby;

        //data
        TerrainData DisplayedTerrain;
        int LevelOfDetail = 2;    //pocet bodov zjemnenia medzi 2 vstupnymi bodmi, LOD=0 su vstupne data, medzi 2 vstupmnymi bodmi bude LOD bodov

        public MainWindow()
        {
            InitializeComponent();

            //UI:
            IsRightDown = false;

            //TU NASTAVIT CO SA CHCEME ZOBRAZIT
            show_Axes = true;
            show_Points = true;
            show_Wireframe = false;
            show_Quads = true;
            point_color_gradients = false;
            quad_color_gradient = true;

            //Spracovanie
            TerrainData MatlabDataSet1 = new MatlabTerrainData(typInterpolacie.NEINTERPOLUJ,LevelOfDetail, "TerrainSample2022-11-02.txt", 256);  // subor sa nachadza v bin/debug
            TerrainData  HeightmapData1 = new HeightmapTerrainData( typInterpolacie.CATMULLROM, LevelOfDetail, "HeightmapSmaller.png");
            TerrainData GeoTiff1 = new GeoTiffTerrainData(typInterpolacie.CATMULLROM, LevelOfDetail, "2022-12-03TIFYn48_e017_1arc_v3.tif_900.txt", 30, 27);

            //Ktory sa ma zobrazit
            //DisplayedTerrain = MatlabDataSet1;
            //DisplayedTerrain = HeightmapData1;
            DisplayedTerrain = GeoTiff1;

        }




        //-----------------------------------------------------------------------------------------------------------------------

        /////////////////////////////////////////////////////////
        //                                                     //
        //                  DRAWING PROCEDURES                 //
        //                                                     //
        /////////////////////////////////////////////////////////


        private void DrawAxes()
        {
            GL.Begin(PrimitiveType.Lines);
            GL.Color3(1.0f, 0.0f, 0.0f);
            GL.Vertex3(0.0f, 0.0f, 0.0f);
            GL.Color3(1.0f, 0.0f, 0.0f);
            GL.Vertex3(2.0f, 0.0f, 0.0f);

            GL.Color3(0.0f, 1.0f, 0.0f);
            GL.Vertex3(0.0f, 0.0f, 0.0f);
            GL.Color3(0.0f, 1.0f, 0.0f);
            GL.Vertex3(0.0f, 2.0f, 0.0f);

            GL.Color3(0.0f, 0.0f, 1.0f);
            GL.Vertex3(0.0f, 0.0f, 0.0f);
            GL.Color3(0.0f, 0.0f, 1.0f);
            GL.Vertex3(0.0f, 0.0f, 2.0f);
            GL.End();
        }


        //-----------------------------------------------------------------------------------------------------------------------
        //                                                      DRAWING
        //-----------------------------------------------------------------------------------------------------------------------




        // initialization of the window, where OpenTK drawing is used 
        private void WindowsFormsHost_Initialized(object sender, EventArgs e)
        {
            // Inicializacia OpenTK;
            OpenTK.Toolkit.Init();
            var flags = GraphicsContextFlags.Default;
            glControl = new GLControl(new GraphicsMode(32, 24), 2, 0, flags);
            glControl.MakeCurrent();
            glControl.Paint += GLControl_Paint;
            glControl.Dock = DockStyle.Fill;
            (sender as WindowsFormsHost).Child = glControl;

            // user controls
            glControl.MouseDown += GLControl_MouseDown;
            glControl.MouseMove += GLControl_MouseMove;
            glControl.MouseUp += GLControl_MouseUp;
            glControl.MouseWheel += GLControl_MouseWheel;

            // shading
            //GL.ShadeModel(ShadingModel.Smooth);

            // color of the window
            GL.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);

            GL.ClearDepth(1.0f);

            //enable z-buffering
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            //smoothing
            GL.Enable(EnableCap.LineSmooth);
            GL.Enable(EnableCap.PointSmooth);

            // illumination
            float[] light_ambient = { 0.5f, 0.5f, 0.5f, 1.0f };
            float[] light_diffuse = { 0.5f, 0.5f, 0.5f, 1.0f };
            float[] light_specular = { 0.2f, 0.2f, 0.2f, 1.0f };
            float[] light_position = { 10.0f, 10.0f, 200.0f };
            GL.Light(LightName.Light0, LightParameter.Ambient, light_ambient);
            GL.Light(LightName.Light0, LightParameter.Diffuse, light_diffuse);
            GL.Light(LightName.Light0, LightParameter.Specular, light_specular);
            GL.Light(LightName.Light0, LightParameter.ConstantAttenuation, 1.0f);
            GL.Light(LightName.Light0, LightParameter.Position, light_position);
            GL.Enable(EnableCap.Light0);

            // parameters for the camera
            Phi = 0.6f; Theta = 0.6f; Dist = 3.8f;

            //farby terénu
            FarebnaLegendaHodnoty = new float[] { -0.5f, 200, 500, 1000, 1500 };
            FarebnaLegendaFarby = new float[][] {
                new float[3] { 0.0f, 0.4f, 0.55f }, //modrá
                new float[3] { 0.54f, 0.77f, 0.36f }, //zelená
                new float[3] { 0.95f, 0.89f, 0.33f },//žltá
                new float[3] { 0.95f, 0.75f, 0.4f },//svetlohnedá
                new float[3] { .8f, .44f, .28f },//hnedá
                new float[3] { .51f, .28f, .23f }   //tmavohnedá
            };
            if (FarebnaLegendaHodnoty.Length + 1 != FarebnaLegendaFarby.Length)
            {
                msg.MessageBox.Show("Počet výškových hodnôt legendy musí byť o 1 menší ako počet farieb v legende. Program sa skončí.");
            }
        }

        // drawing 
        private void GLControl_Paint(object sender, PaintEventArgs e)
        {
            // Modelview matrix
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            Matrix4 matLook = Matrix4.LookAt((float)(Dist * Math.Cos(Theta) * Math.Cos(Phi)), (float)(Dist * Math.Sin(Phi) * Math.Cos(Theta)), (float)(Dist * Math.Sin(Theta)), 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f);
            GL.LoadMatrix(ref matLook);

            // perspective projection
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            Matrix4 matPers = Matrix4.CreatePerspectiveFieldOfView(0.785f, (float)glControl.Width / (float)glControl.Height, 0.1f, 10.5f);
            GL.LoadMatrix(ref matPers);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            //TU SA KRESLIA PRIMITIVY BEZ MATERIALU
            if (show_Axes) DrawAxes();
            //if (show_Points) DrawPoints(DisplayedTerrain.InterpolationPoints, point_color_gradients);
            if (show_Points) DrawPoints(DisplayedTerrain.InputDataPoints, point_color_gradients);
            if (show_Wireframe) DrawWireframe(DisplayedTerrain.InterpolationPoints);

            GL.Enable(EnableCap.Lighting);
            GL.Enable(EnableCap.DepthTest);

            //TU SA KRESLIA PRIMITIVY S MATERIALOM
            if (show_Quads) DrawQuads(DisplayedTerrain.InterpolationPoints, quad_color_gradient);

            GL.Disable(EnableCap.Lighting);

            // the buffers need to swapped, so the scene is drawn, kvoli float bufferu
            glControl.SwapBuffers();
        }

        public void DrawPoints(Vector3[,] Vertices, bool color_gradient)
        {
            //zdroj https://gdbooks.gitbooks.io/legacyopengl/content/Chapter3/Points.html

            GL.PointSize(5.0f); //zmenu vlastnosti davaj pred begin
            float[] point_color = { 0.3f, 0.3f, 0.3f };

            GL.Begin(PrimitiveType.Points);
            for (int j = 0; j < Vertices.GetLength(1); j++)
            {
                for (int i = 0; i < Vertices.GetLength(0); i++)
                {
                    if (color_gradient) GL.Color3(VertexColorByLegend(Vertices[i, j]));
                    else GL.Color3(point_color);

                    GL.Vertex3(SaT(Vertices[i, j]));
                }
            }
            GL.End();
        }

        public void DrawWireframe(Vector3[,] Vertices)
        {
            //pospajam body useckami v u-smere, useckami vo v-smere

            float[] wire_color = { 0.0f, 0.2f, 0.12f };

            GL.Begin(PrimitiveType.Lines);
            for (int j = 0; j < Vertices.GetLength(1); j++)
            {
                for (int i = 0; i < Vertices.GetLength(0); i++)
                {
                    if (i < Vertices.GetLength(0) - 1)
                    {
                        //u smer
                        GL.Color3(wire_color);
                        GL.Vertex3(SaT(Vertices[i, j]));
                        GL.Color3(wire_color);
                        GL.Vertex3(SaT(Vertices[i + 1, j]));
                    }

                    if (j < Vertices.GetLength(1) - 1)
                    {
                        //v smer
                        GL.Color3(wire_color);
                        GL.Vertex3(SaT(Vertices[i, j]));
                        GL.Color3(wire_color);
                        GL.Vertex3(SaT(Vertices[i, j + 1]));
                    }
                }
            }
            GL.End();
        }

        public void DrawQuads(Vector3[,] Vertices, bool color_gradient)
        {
            //4. TODO vytvorim plochy medzi stvrocekami
            //https://registry.khronos.org/OpenGL-Refpages/gl2.1/xhtml/glPolygonMode.xml
            //https://www.khronos.org/opengl/wiki/How_lighting_works


            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill); // enable filling of shapes with color 

            float[] amb_color = { 0.69f, 0.4f, 0.1f };
            float[] diff_color = { 0.9f, 0.9f, 0.9f, 1.0f };
            float[] spec_color = { 0.5f, 0.5f, 0.5f, 1.0f };

            GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Ambient, diff_color);
            GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Specular, spec_color);
            GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Shininess, 0.1f);

            GL.Begin(PrimitiveType.Quads);
            for (int j = 0; j < Vertices.GetLength(1) - 1; j++)
            {
                for (int i = 0; i < Vertices.GetLength(0) - 1; i++)
                {
                    GL.Normal3(ComputeQuadNormalVector(Vertices[i, j], Vertices[i + 1, j], Vertices[i, j + 1]));

                    if (color_gradient)
                    {
                        GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Ambient, VertexColorByLegend(Vertices[i, j]));
                        GL.Vertex3(SaT(Vertices[i, j]));
                        GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Ambient, VertexColorByLegend(Vertices[i + 1, j]));
                        GL.Vertex3(SaT(Vertices[i + 1, j]));
                        GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Ambient, VertexColorByLegend(Vertices[i + 1, j + 1]));
                        GL.Vertex3(SaT(Vertices[i + 1, j + 1]));
                        GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Ambient, VertexColorByLegend(Vertices[i, j + 1]));
                        GL.Vertex3(SaT(Vertices[i, j + 1]));
                    }
                    else
                    {
                        GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Ambient, amb_color);
                        GL.Vertex3(Vertices[i, j]);
                        GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Ambient, amb_color);
                        GL.Vertex3(Vertices[i + 1, j]);
                        GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Ambient, amb_color);
                        GL.Vertex3(Vertices[i + 1, j + 1]);
                        GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Ambient, amb_color);
                        GL.Vertex3(Vertices[i, j + 1]);
                    }
                }
            }
            GL.End();
        }

        private Vector3 SaT(Vector3 vertex)  //Scale and Translate terrain according to the dimension of terrain model
        {
            return (vertex + DisplayedTerrain.posunutie) * DisplayedTerrain.skalovanie;
        }

        private float[] VertexColorByLegend(Vector3 Vertex)    //priradi bodu (s naozajstvou, netransformovanou hodnotou !!!) farbu podla legendy 
        {
            float h = (float)Vertex.Z;      //nadmorska vyska bodu

            if (h < FarebnaLegendaHodnoty[0])   //všetky nižšie ako najnizsia hodnota
            {
                return FarebnaLegendaFarby[0];
            }

            for (int i = 1; i < FarebnaLegendaHodnoty.Length; i++)
            {
                if (h < FarebnaLegendaHodnoty[i])
                {
                    float p = ((float)h - FarebnaLegendaHodnoty[i - 1]) / (FarebnaLegendaHodnoty[i] - FarebnaLegendaHodnoty[i - 1]);  //výpočet parametra (pozície) na rovnomerne parametrizovanej úsečke medzi 2 v hodnotami legendy
                    RecomputeEaseInEaseOutParam(ref p);

                    float r = (1 - p) * FarebnaLegendaFarby[i - 1][0] + p * FarebnaLegendaFarby[i][0];     //red
                    float g = (1 - p) * FarebnaLegendaFarby[i - 1][1] + p * FarebnaLegendaFarby[i][1];      //green
                    float b = (1 - p) * FarebnaLegendaFarby[i - 1][2] + p * FarebnaLegendaFarby[i][2];      //blue

                    return new float[3] { r, g, b };
                }
            }

            return FarebnaLegendaFarby.Last();  //všetko vyššie ako najvyssia hodnota
        }

        private float RecomputeEaseInEaseOutParam(ref float p)     //pretransformuje parameter, z linearnej parametrizacie na ease in ease out; iba pre lepsi farebny efekts
        {
            //zdroj: https://easings.net/#easeInOutQuad

            if (p < 0.5) return 2 * p * p;
            //else
            return 1 - (float)Math.Pow(-2 * p + 2, 2) / 2;
        }

        private Vector3 ComputeQuadNormalVector(Vector3 V00, Vector3 V01, Vector3 V10)  //vypocita normalu pre plochu danu vektormi V10-V00 a V01-V00
        {
            //normalove vektory su pocitane pre vsetky stvorceky, kt pocet je v danom smere o 1 menej ako bodov
            Vector3 u = V10 - V00;
            Vector3 v = V01 - V00;
            Vector3 c = Vector3.Cross(u, v);
            c.Normalize();
            return c;
        }


        //-----------------------------------------------------------------------------------------------------------------------



        //-----------------------------------------------------------------------------------------------------------------------

        /////////////////////////////////////////////////////////
        //                                                     //
        //                 USER INTERFACE CONTROLS             //
        //                                                     //
        /////////////////////////////////////////////////////////
        private void GLControl_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {

            if (e.Button == MouseButtons.Right) // camera is adjusted using RMB
            {
                IsRightDown = true;
                RightX = e.X;
                RightY = e.Y;
                oPhi = Phi;                 //zapamata sa aktulne otocenie, s oPhi sa bude pracovat pri posunavi kurzora
                oTheta = Theta;
            }
            glControl.Invalidate();
        }

        private void GLControl_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            Dist -= (double)e.Delta * 0.001; // zooming
            glControl.Invalidate();
        }

        private void GLControl_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (IsRightDown) // RMB - rotate the camera
            {
                IsRightDown = true;

                Phi = oPhi + (RightX - e.X) / 200.0f;
                Theta = oTheta + (e.Y - RightY) / 200.0f;
            }

            glControl.Invalidate();
        }

        private void GLControl_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right) IsRightDown = false;
        }

        //-----------------------------------------------------------------------------------------------------------------------

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //naskaluje viewport na velkost zaciatocneho okna
            GL.Viewport(0, 0, glControl.Width, glControl.Height);
        }
    }
}