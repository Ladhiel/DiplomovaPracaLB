using System;
using System.Windows;
using g3;
using OpenTK;


namespace DiplomovaPracaLB
{
    public abstract partial class Splajn
    {
        protected int LOD;
        protected int m, n; //pocet vrcholov v zjemnenom vzorkovani      indexy od 0 po m-1
        protected Vector4[,] InterpolationPoints;
        public Vector4[,] TmpPoints;
        private Vector3[,] Normals;              //normalove vektory v lavych dolnych rohov jemneho vzorkovania
        private double[,] ErrorValues;
        public bool isRBF = false;
        private double RMSE = 0;


        public void Interpolate(ref TerrainData RefTerrain)
        {
            if (ValidateDimensions())
            {
                InterpolationPoints = CreateInterpolationPoints(ref RefTerrain.WeightedDataPointsSample);
                ComputeNormals(InterpolationPoints);
                Evaluate(ref RefTerrain);
            }
        }

        public void ReInterpolate(ref TerrainData RefTerrain, int new_LOD)
        {
            LoadDimensions(new_LOD, RefTerrain.GetSampleSize());
            if (ValidateDimensions())
            {
                Interpolate(ref RefTerrain);
            }
        }

        protected virtual void LoadDimensions(int _Level_Of_Detail, int[] InputSize)
        {
            //kazdy splajn svoje 
        }

        protected virtual Vector4[,] CreateInterpolationPoints(ref Vector4[,] Vector)
        {
            //kazdy splajn svoje
            Vector4[,] IP = new Vector4[m, n];
            return IP;
        }

        public Vector3 Rational(Vector4 vertex)
        {
            //z homogennych do afinnych suradnic
            if (vertex[3] == 0)
            {
                MessageBox.Show("pozor, bod ma nulovu vahu. Riadok 52 v Splajn.cs");
                return Vector3.Zero;
            }
            if (vertex[3] == 1)
            {
                return new Vector3(vertex);     //len za zabudne w-suradnica
            }

            return new Vector3(vertex[0] / vertex[3], vertex[1] / vertex[3], vertex[2] / vertex[3]);
        }

        protected void ComputeNormals(Vector4[,] Vertices)
        {
            if (m < 1 || n < 1)
            {
                Normals = new Vector3[0, 0];
                return;
            }

            Normals = new Vector3[m - 1, n - 1];

            for (int i = 0; i < m - 1; i++)
            {
                for (int j = 0; j < n - 1; j++)
                {
                    Normals[i, j] = ComputeNormalVectorInPoint(Vertices[i, j], Vertices[i, j + 1], Vertices[i + 1, j]);
                }
            }
        }

        private Vector3 ComputeNormalVectorInPoint(Vector4 V00, Vector4 V01, Vector4 V10)  //vypocita normalu pre plochu/bod danu vektormi V10-V00 a V01-V00
        {
            //normalove vektory su pocitane pre vsetky stvorceky, kt pocet je v danom smere o 1 menej ako bodov

            Vector3 u = Rational(V10) - Rational(V00);  //musim previes na vahu 1, aby rozdiel bol vektor
            Vector3 v = Rational(V01) - Rational(V00);
            Vector3 c = Vector3.Cross(u, v);
            //niekedy su vrcholy usporiadane zostupne a niekedy opacne v datasete, ale chcem aby normaly smerovali "hore. nemam taky pripad datasetu, ze by tam boli previsy
            //test, ci zvieraju ostry uhol, tj ked skal.sucin je >0 (ked =0, tak je jedno)
            if (Vector3.Dot(c, Vector3.UnitZ) < 0)
            {
                c = Vector3.Cross(v, u);
            }
            c.Normalize();
            return c;
        }

        public void Evaluate(ref TerrainData RefTerrain)
        {
            int dens = RefTerrain.GetDensity();
            if (dens != LOD + 1) return;

            TmpPoints = new Vector4[m, n];
            ErrorValues = new double[m, n];

            //BuildAABB using g3shapr library
            g3.DMeshAABBTree3 Spatial = new DMeshAABBTree3(RefTerrain.MeshDataAll);
            Spatial.Build();

            //Find collided triangle
            g3.Vector3f RayDir = new Vector3f(0, 0, 1);
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    //Ray parallel to z axis

                    Vector3 RatIP = Rational(InterpolationPoints[i, j]);
                    g3.Ray3d ZRay = new Ray3d(new Vector3f(RatIP.X, RatIP.Y, RefTerrain.GetMinMaxVal(false, 2) - 1), RayDir);

                    //Collide ray and mesh
                    int hit_tid = Spatial.FindNearestHitTriangle(ZRay);

                    //If collided
                    if (hit_tid != DMesh3.InvalidID)
                    {
                        IntrRay3Triangle3 Intersection = MeshQueries.TriangleIntersection(RefTerrain.MeshDataAll, hit_tid, ZRay);

                        //Result = point on mesh
                        TmpPoints[i, j] = new Vector4(
                            (float)ZRay.PointAt(Intersection.RayParameter).x,
                            (float)ZRay.PointAt(Intersection.RayParameter).y,
                            (float)ZRay.PointAt(Intersection.RayParameter).z,
                            1.0f);

                        ErrorValues[i, j] = Math.Abs(TmpPoints[i, j].Z - RatIP.Z);

                        if (ErrorValues[i, j]==0)
                        {

                        }
                    }
                }
            }

            RMSE = CalculateRMSE();
        }

        private double CalculateRMSE()
        {
            //RMSE = sqrt((1/N)*SUM{1 to N}(error^2))

            int N = ErrorValues.GetLength(0) * ErrorValues.GetLength(1);
            double sum_of_squared_errors = 0;

            for (int i = 0; i < ErrorValues.GetLength(0); i++)
            {
                for (int j = 0; j < ErrorValues.GetLength(1); j++)
                {
                    sum_of_squared_errors += ErrorValues[i, j] * ErrorValues[i, j];
                }
            }

            return Math.Sqrt(sum_of_squared_errors / N);
        }

        private bool ValidateDimensions()
        {
            if (m < 0 || n < 0)
            {
                m = 0; n = 0;
                MessageBox.Show("Not enough points in dataset to create intepolation. Decrease density or pick some bigger dataset.");
                return false;
            }
            return true;
        }

        public ref Vector4[,] GetPoints()
        {
            return ref InterpolationPoints;
        }

        public ref Vector3[,] GetNormals()
        {
            return ref Normals;
        }

        public int GetSize(int k)
        {
            if (k == 0) return m;
            if (k == 1) return n;
            return 0;
        }

        public double GetErrorValue(int i, int j)
        {
            if (ErrorValues != null)
            {
                if (0 <= i && i <= m && 0 <= j && j <= n)
                {
                    return ErrorValues[i, j];
                }
            }
            return 0;
        }

        public double GetRMSE()
        {
            return RMSE;
        }


    }
}
