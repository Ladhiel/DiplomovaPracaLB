using System;
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
using swf = System.Windows.Forms;   //povenoala som kvoli konfliktnemu pomenovanius
using System.Windows.Forms.Integration;
using System.IO;
using sd = System.Drawing;

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
        bool IsRightDown, IsLeftDown;
        int ActivePoint_m_index, ActivePoint_n_index;

        //light settings
        float[] light_ambient, light_diffuse, light_specular;
        float light_dist = 8.0f, light_r = 10.0f /*100*/, light_eps = 1.0f, default_dist = 4.8f;
        float[] light_position = { 1.0f, 1.0f, 1.0f };
        float[][] LightPositionsAboveModelHemiSphere;

        //UI settings
       
        float[] FarebnaLegendaHodnoty;
        float[][] FarebnaLegendaFarby;
        float pointsize;
        bool show_Axes, show_Points, show_Wireframe, show_Quads;

        //data
        TerrainData DisplayedTerrain;
        TypeOfShading selectedShadingType;
        int LevelOfDetail;    //pocet bodov zjemnenia medzi 2 vstupnymi bodmi, LOD=0 su vstupne data, medzi 2 vstupmnymi bodmi bude LOD bodov

        private enum TypeOfShading
        {
            FLAT,
            GOURAUD
        };

        public MainWindow()
        {
            InitializeComponent();

            //UI:
            IsRightDown = false;
            IsLeftDown = false;
            

            //TU NASTAVIT CO SA CHCEME ZOBRAZIT
            show_Axes = false;
            show_Points = true;
            show_Wireframe = false;
            show_Quads = true;

            selectedShadingType = TypeOfShading.FLAT;
            LevelOfDetail = 3;
            TextBox_LOD.Text = LevelOfDetail.ToString();

            //Spracovanie
            // TerrainData MatlabDataSet1 = new MatlabTerrainData(typInterpolacie.NEINTERPOLUJ,LevelOfDetail, "TerrainSample2022-11-02.txt", 256);  // subor sa nachadza v bin/debug
            //TerrainData  HeightmapData1 = new HeightmapTerrainData( typInterpolacie.CATMULLROM, LevelOfDetail, "HeightmapSmaller.png");
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
            glControl.Dock = swf.DockStyle.Fill;
            (sender as WindowsFormsHost).Child = glControl;

            // user controls
            glControl.MouseDown += GLControl_MouseDown;
            glControl.MouseMove += GLControl_MouseMove;
            glControl.MouseUp += GLControl_MouseUp;
            glControl.MouseWheel += GLControl_MouseWheel;

            // shading
            if(selectedShadingType == TypeOfShading.FLAT)
            {
                GL.ShadeModel(ShadingModel.Flat);
            }
            else 
            {
                GL.ShadeModel(ShadingModel.Smooth);
            }   

            // color of the window
            GL.ClearColor(0.5f, 0.5f, 0.5f, 1.0f);
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
            light_ambient = new float[] { 0.9f, 0.9f, 0.9f, 1.0f };
            light_diffuse = new float[] { 0.5f, 0.5f, 0.5f, 1.0f };
            light_specular = new float[] { 0.2f, 0.2f, 0.2f, 0.8f };

            GL.Light(LightName.Light0, LightParameter.Ambient, light_ambient);
            GL.Light(LightName.Light0, LightParameter.Diffuse, light_diffuse);
            GL.Light(LightName.Light0, LightParameter.Specular, light_specular);
            GL.Light(LightName.Light0, LightParameter.ConstantAttenuation, 1.0f);
            GL.Light(LightName.Light0, LightParameter.Position, light_position);
            GL.Enable(EnableCap.Light0);

            float s = 0.7071067811865475244f;
            LightPositionsAboveModelHemiSphere = new float[][] {
                new float[3] { -s * light_r,  s * light_r, light_dist},
                new float[3] {         0.0f,      light_r, light_dist},
                new float[3] {  s * light_r,  s * light_r, light_dist},
                new float[3] {    - light_r,         0.0f, light_dist},
                new float[3] {         0.0f,         0.0f, light_dist + light_r*light_eps},
                new float[3] {      light_r,         0.0f, light_dist},
                new float[3] { -s * light_r, -s * light_r, light_dist},
                new float[3] {         0.0f,     -light_r, light_dist},
                new float[3] {  s * light_r, -s * light_r, light_dist}
            };

            // parameters for the camera
            Phi = -0.6f; Theta = 0.3f; Dist = default_dist;
            pointsize = ChangePointSize();

            //farby terénu
            FarebnaLegendaHodnoty = new float[] { -0.5f, 200, 500, 1000, 1500 };
            FarebnaLegendaFarby = new float[][] {
                new float[3] { 0.0f, 0.4f, 0.55f }, //modrá
                new float[3] { 0.34f, 0.57f, 0.16f }, //zelená
                new float[3] { 0.95f, 0.89f, 0.33f },//žltá
                new float[3] { 0.95f, 0.75f, 0.4f },//svetlohnedá
                new float[3] { .8f, .44f, .28f },//hnedá
                new float[3] { .51f, .28f, .23f }   //tmavohnedá
            };
            if (FarebnaLegendaHodnoty.Length + 1 != FarebnaLegendaFarby.Length)
            {
                MessageBox.Show("Počet výškových hodnôt legendy musí byť o 1 menší ako počet farieb v legende. Program sa skončí.");
            }
        }

        // drawing 
        private void GLControl_Paint(object sender, swf.PaintEventArgs e)
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
            if (show_Points) DrawPoints(DisplayedTerrain.InputDataPoints);
            if (show_Wireframe) DrawWireframe(DisplayedTerrain.InterpolationPoints);
            //DrawPositionLight();

            GL.Enable(EnableCap.Lighting);
            GL.Enable(EnableCap.DepthTest);

            //TU SA KRESLIA PRIMITIVY S MATERIALOM
            if (show_Quads) DrawQuads(DisplayedTerrain.InterpolationPoints, DisplayedTerrain.Normals);

            GL.Disable(EnableCap.Lighting);

            // the buffers need to swapped, so the scene is drawn, kvoli float bufferu
            glControl.SwapBuffers();

            //testovacie
            TextBox1.Text = "m:" + ActivePoint_m_index.ToString();
            TextBox2.Text = "n:" + ActivePoint_n_index.ToString();

        }


        public void DrawPositionLight()
        {
            // float[] light_position = { 10.0f, 10.0f, 200.0f };
            float[] origin = { 0.0f, 0.0f, 0.0f };

            float[] light_color = { 1.0f, 0.95f, 0.0f };
            GL.Begin(PrimitiveType.Lines);
            GL.Color3(light_color);
            GL.Vertex3(light_position);
            GL.Vertex3(origin);
            GL.End();
        }

        public void DrawPoints(Vector3[,] Vertices)
        {
            //zdroj https://gdbooks.gitbooks.io/legacyopengl/content/Chapter3/Points.html

            Vector3 z_eps = new Vector3(0.0f, 0.0f, 1.85f);

            float[] point_color = { 0.3f, 0.3f, 0.3f };
            GL.Color3(point_color);
            GL.PointSize(pointsize);

            GL.Begin(PrimitiveType.Points);

            for (int j = 0; j < Vertices.GetLength(1); j++)
            {
                for (int i = 0; i < Vertices.GetLength(0); i++)
                {
                    GL.Vertex3(SaT(Vertices[i, j] + z_eps));
                }
            }
            GL.End();

            //kresli vybraty bod
            GL.PointSize(2.0f * pointsize);
            GL.Color3(0.85f, 0.53f, 0.10f); //highlight
            GL.Begin(PrimitiveType.Points);
            Vector3 picked = DisplayedTerrain.InputDataPoints[ActivePoint_m_index, ActivePoint_n_index];
            GL.Vertex3(SaT(picked + z_eps));

            GL.End();
        }

        public void DrawWireframe(Vector3[,] Vertices)
        {
            //pospajam body useckami v u-smere, useckami vo v-smere

            float[] wire_color = { 0.0f, 0.2f, 0.12f };
            Vector3 z_eps = new Vector3(0.0f, 0.0f, 0.5f);

            GL.LineWidth(1.2f);
            GL.Begin(PrimitiveType.Lines);
            for (int j = 0; j < Vertices.GetLength(1); j++)
            {
                for (int i = 0; i < Vertices.GetLength(0); i++)
                {
                    if (i < Vertices.GetLength(0) - 1)
                    {
                        //u smer
                        GL.Color3(wire_color);
                        GL.Vertex3(SaT(Vertices[i, j] + z_eps));
                        GL.Color3(wire_color);
                        GL.Vertex3(SaT(Vertices[i + 1, j] + z_eps));
                    }

                    if (j < Vertices.GetLength(1) - 1)
                    {
                        //v smer
                        GL.Color3(wire_color);
                        GL.Vertex3(SaT(Vertices[i, j] + z_eps));
                        GL.Color3(wire_color);
                        GL.Vertex3(SaT(Vertices[i, j + 1] + z_eps));
                    }
                }
            }
            GL.End();
        }

        public void DrawQuads(Vector3[,] Vertices, Vector3[,] Normals)
        {
            //4. TODO vytvorim plochy medzi stvrocekami
            //https://registry.khronos.org/OpenGL-Refpages/gl2.1/xhtml/glPolygonMode.xml
            //https://www.khronos.org/opengl/wiki/How_lighting_works


            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill); // enable filling of shapes with color 
            
            float[] diff_color = { 0.9f, 0.9f, 0.9f, 1.0f };
            float[] spec_color = { 0.5f, 0.5f, 0.5f, 1.0f };

            GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Diffuse, diff_color);
            GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Specular, spec_color);
            GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Shininess, 0.1f);

            int M = Vertices.GetLength(0) - 1;
            int N = Vertices.GetLength(1) - 1;

            GL.Begin(PrimitiveType.Quads);
            for (int j = 0; j < N; j++)
            {
                for (int i = 0; i < M; i++)
                {
                    if (selectedShadingType == TypeOfShading.FLAT)
                    {
                        //rovanaka farba aj normala pre vsetky 4 rohy
                        //defaultne berie farbu z posledneho vrchola
                        GL.Normal3(Normals[i, j]);
                        GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Ambient, VertexColorByLegend((Vertices[i, j]+ Vertices[i + 1, j]+ Vertices[i + 1, j + 1]+ Vertices[i, j + 1])/4));
                        GL.Vertex3(SaT(Vertices[i, j]));
                        GL.Vertex3(SaT(Vertices[i + 1, j]));
                        GL.Vertex3(SaT(Vertices[i + 1, j + 1]));
                        GL.Vertex3(SaT(Vertices[i, j + 1]));
                    }
                    else
                    {
                        //ina farba aj normala pre kazdy z rohov
                        int a = 1;
                        int b = 1;
                        if (i == M - 1) a = 0;  //okrajove body nemaju vypocitanu vlastnu normalu, pozicaju si ju od predchadzajuceho bodu
                        if (j == N - 1) b = 0;
                        GL.Normal3(Normals[i, j]);
                        GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Ambient, VertexColorByLegend(Vertices[i, j]));
                        GL.Vertex3(SaT(Vertices[i, j]));

                        GL.Normal3(Normals[i + a, j]);
                        GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Ambient, VertexColorByLegend(Vertices[i + 1, j]));
                        GL.Vertex3(SaT(Vertices[i + 1, j]));

                        GL.Normal3(Normals[i + a, j + b]);
                        GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Ambient, VertexColorByLegend(Vertices[i + 1, j + 1]));
                        GL.Vertex3(SaT(Vertices[i + 1, j + 1]));

                        GL.Normal3(Normals[i, j + b]);
                        GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Ambient, VertexColorByLegend(Vertices[i, j + 1]));
                        GL.Vertex3(SaT(Vertices[i, j + 1]));
                    }
                }
            }
            GL.End();
        }

        private float ChangePointSize()
        {
            return (float)(1 / Math.Sqrt(Dist)) * 3.0f * default_dist;
        }

        private Vector3 SaT(Vector3 vertex)  //Scale and Translate terrain according to the dimension of terrain model
        {
            return (vertex + DisplayedTerrain.posunutie) * 2.5f * DisplayedTerrain.skalovanie;
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

       



        //-----------------------------------------------------------------------------------------------------------------------


       
        //-----------------------------------------------------------------------------------------------------------------------

        /////////////////////////////////////////////////////////
        //                                                     //
        //                 USER INTERFACE CONTROLS             //
        //                                                     //
        /////////////////////////////////////////////////////////

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
        private void Button_ResetView_Click(object sender, RoutedEventArgs e)
        {
            Phi = -0.6f; Theta = 0.3f; Dist = 3.8f;
            pointsize = ChangePointSize();
            glControl.Invalidate();
        }

        private void Button_ShowAxes_Click(object sender, RoutedEventArgs e)
        {
            if (show_Axes)
            {
                show_Axes = false;
                Button_ShowAxes.Background = new SolidColorBrush(Color.FromRgb(191, 200, 191));
            }
            else
            {
                show_Axes = true;
                Button_ShowAxes.Background = new SolidColorBrush(Color.FromRgb(96, 117, 96));
            }
            glControl.Invalidate();
        }

        private void Button_ShowPoints_Click(object sender, RoutedEventArgs e)
        {
            if (show_Points)
            {
                show_Points = false;
                Button_ShowPoints.Background = new SolidColorBrush(Color.FromRgb(191, 200, 191));
            }
            else
            {
                show_Points = true;
                Button_ShowPoints.Background = new SolidColorBrush(Color.FromRgb(96, 117, 96));
            }
            glControl.Invalidate();
        }

        private void Button_ShowWireframe_Click(object sender, RoutedEventArgs e)
        {
            if (show_Wireframe)
            {
                show_Wireframe = false;
                Button_ShowWireframe.Background = new SolidColorBrush(Color.FromRgb(191, 200, 191));
            }
            else
            {
                show_Wireframe = true;
                Button_ShowWireframe.Background = new SolidColorBrush(Color.FromRgb(96, 117, 96));
            }
            glControl.Invalidate();
        }

        private void Button_ShowQuads_Click(object sender, RoutedEventArgs e)
        {
            if (show_Quads)
            {
                show_Quads = false;
                Button_ShowQuads.Background = new SolidColorBrush(Color.FromRgb(191, 200, 191));
            }
            else
            {
                show_Quads = true;
                Button_ShowQuads.Background = new SolidColorBrush(Color.FromRgb(96, 117, 96));
            }
            glControl.Invalidate();
        }
       
        private void TextBox_LOD_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)    //Enter
            {
                int new_LOD;
                try
                {
                    new_LOD = int.Parse(TextBox_LOD.Text);
                    ChangeLevelOfDetail(new_LOD);
                }
                catch
                {
                    MessageBox.Show("Nesprávny vstup!");
                    TextBox_LOD.Text = LevelOfDetail.ToString();
                }
            }
        }

        private void Button_LODminus_Click(object sender, RoutedEventArgs e)
        {
            ChangeLevelOfDetail(LevelOfDetail - 1);
        }

        private void Button_LODplus_Click(object sender, RoutedEventArgs e)
        {
            ChangeLevelOfDetail(LevelOfDetail + 1);
        }

        private void ChangeLevelOfDetail(int new_LOD)
        {
            if (new_LOD >= 0 && new_LOD <= 10)
            {
                LevelOfDetail = new_LOD;
                TextBox_LOD.Text = new_LOD.ToString();      //toto je len pre buttony; pre textbox sa nic nezmeni, ale lepsie prepisat na to iste, nez na zle a zase naspat.
                //treba prepocitat navyorkovanie splajnu
                DisplayedTerrain.ReInterpolate(new_LOD);
                glControl.Invalidate();
            }
            else
            {
                TextBox_LOD.Text = LevelOfDetail.ToString();  //do TextBoxu napisem ostavajucu hodnotu LOD
            }
        }

        private void Slider_ChangeLightIntensity(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            float slider_value = (float)(Slider_LightIntensity.Value / 100);  //intenzita v percentach
            float[] la = new float[] { light_ambient[0] * slider_value, light_ambient[1] * slider_value, light_ambient[2] * slider_value, 1.0f };
            float[] ld = new float[] { light_diffuse[0] * slider_value, light_diffuse[1] * slider_value, light_diffuse[2] * slider_value, 1.0f };
            float[] ls = new float[] { light_specular[0] * slider_value, light_specular[1] * slider_value, light_specular[2] * slider_value, 0.8f };
            GL.Light(LightName.Light0, LightParameter.Ambient, la);
            GL.Light(LightName.Light0, LightParameter.Diffuse, ld);
            GL.Light(LightName.Light0, LightParameter.Specular, ls);
            glControl.Invalidate();
        }

        private void RadioButton_ChangeLightPosition_Checked(object sender, RoutedEventArgs e)
        {
            string s = (sender as RadioButton).Name;
            int i = int.Parse(s.Substring(s.Length - 1)) - 1;   //posledny znak v nazve je indexpozicie

            light_position = LightPositionsAboveModelHemiSphere[i];
            GL.Light(LightName.Light0, LightParameter.Position, light_position);
            glControl.Invalidate();
        }

        private void RadioButton_ShadingFlat_Checked(object sender, RoutedEventArgs e)
        {
            selectedShadingType = TypeOfShading.FLAT;
            glControl.Invalidate();
        }

        private void RadioButton_ShadingGouraud_Checked(object sender, RoutedEventArgs e)
        {

            selectedShadingType = TypeOfShading.GOURAUD;
            GL.ShadeModel(ShadingModel.Smooth);
            glControl.Invalidate();
        }

        //---------------------------------------------------------------------------------------------------------------------------------

        public Vector3 UnProject(Vector3 mouse, Matrix4 projection, Matrix4 view, Size viewport)
        {
            Vector4 vec;

            vec.X = 2.0f * mouse.X / (float)viewport.Width - 1;
            vec.Y = -(2.0f * mouse.Y / (float)viewport.Height - 1);
            vec.Z = mouse.Z;
            vec.W = 1.0f;

            Matrix4 viewInv = Matrix4.Invert(view);
            Matrix4 projInv = Matrix4.Invert(projection);

            Vector4.Transform(ref vec, ref projInv, out vec);
            Vector4.Transform(ref vec, ref viewInv, out vec);

            if (vec.W > 0.000001f || vec.W < -0.000001f)
            {
                vec.X /= vec.W;
                vec.Y /= vec.W;
                vec.Z /= vec.W;
            }

            return vec.Xyz;
        }

        private void GLControl_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == swf.MouseButtons.Right) // camera is adjusted using RMB
            {
                IsRightDown = true;
                RightX = e.X;
                RightY = e.Y;
                oPhi = Phi;                 //zapamata sa aktulne otocenie, s oPhi sa bude pracovat pri posunavi kurzora
                oTheta = Theta;
            }
            else if (e.Button == swf.MouseButtons.Left) // using LMB we search for the control point beneath the mouse cursor 
            {
                //the idea of the searching -- when I am doing the inverse projection, what points lie in the ray which is casted from the point beneath the cursor. If there are any, I choose the closest one. 

                Vector3 start, end;

                int[] viewport = new int[4];
                Matrix4 modelMatrix, projMatrix;

                GL.GetFloat(GetPName.ModelviewMatrix, out modelMatrix);
                GL.GetFloat(GetPName.ProjectionMatrix, out projMatrix);
                GL.GetInteger(GetPName.Viewport, viewport);

                start = UnProject(new Vector3(e.X, e.Y, 0.0f), projMatrix, modelMatrix, new Size(viewport[2], viewport[3]));
                end = UnProject(new Vector3(e.X, e.Y, 1.0f), projMatrix, modelMatrix, new Size(viewport[2], viewport[3]));

                double se = Math.Sqrt(Vector3.Dot(start - end, start - end));
                for (int k = 0; k < DisplayedTerrain.InputDataPoints.GetLength(0); k++)
                    for (int i = 0; i < DisplayedTerrain.InputDataPoints.GetLength(1); i++)
                    {
                        double sA = Math.Sqrt(Vector3.Dot(SaT(DisplayedTerrain.InputDataPoints[k, i]) - start, SaT(DisplayedTerrain.InputDataPoints[k, i]) - start));
                        double eA = Math.Sqrt(Vector3.Dot(SaT(DisplayedTerrain.InputDataPoints[k, i]) - end, SaT(DisplayedTerrain.InputDataPoints[k, i]) - end));
                        if (sA + eA > se - 0.001 && sA + eA < se + 0.001)
                        {
                            ActivePoint_m_index = k;
                            ActivePoint_n_index = i;

                            IsLeftDown = true;

                            RightX = e.X;
                            RightY = e.Y;
                        }
                    }
            }
            glControl.Invalidate();
        }

        private void GLControl_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            double new_Dist = Dist - (double)e.Delta * 0.001; // zooming
            if (new_Dist >= 0.001)
            {
                Dist = new_Dist;
                pointsize = ChangePointSize();
            }
            glControl.Invalidate();
        }

        private void GLControl_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (IsRightDown) // RMB - rotate the camera
            {
                IsRightDown = true;

                Phi = oPhi + (RightX - e.X) / 200.0f;
                Theta = oTheta + (e.Y - RightY) / 200.0f;

                //updatuje sa svetlo
                GL.Light(LightName.Light0, LightParameter.Position, light_position);
            }
            else if (IsLeftDown) // LMB - move the control vertex
            {
                IsLeftDown = true;

                RightY = e.Y;
                RightX = e.X;
            }

            glControl.Invalidate();
        }

        private void GLControl_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == swf.MouseButtons.Right) IsRightDown = false;
            if (e.Button == swf.MouseButtons.Left) IsLeftDown = false;
        }

        //-----------------------------------------------------------------------------------------------------------------------

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //naskaluje viewport na velkost zaciatocneho okna
            GL.Viewport(0, 0, glControl.Width, glControl.Height);
        }
    }
}
