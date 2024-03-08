using System;
using System.Windows;
using OpenTK;


namespace DiplomovaPracaLB
{
    public abstract partial class Splajn
    {
        protected int LOD;
        protected int m, n; //pocet vrcholov v zjemnenom vzorkovani      indexy od 0 po m-1
        protected Vector4[,] InterpolationPoints;
        private Vector3[,] Normals;              //normalove vektory v lavych dolnych rohov jemneho vzorkovania
        private float[,] ErrorValues;
        protected bool isRBF = false;

        public void Interpolate(ref TerrainData RefTerrain)
        {
            InterpolationPoints = CreateInterpolationPoints(RefTerrain.WeightedDataPointsSample);
            ComputeNormals(InterpolationPoints);
            Evaluate(ref RefTerrain);
        }

        public void ReInterpolate(ref TerrainData RefTerrain, int new_LOD)
        {
            LoadDimensions(new_LOD, RefTerrain.GetSampleSize());
            Interpolate(ref RefTerrain);
        }

        protected virtual void LoadDimensions(int _Level_Of_Detail, int[] InputSize)
        {
           //kazdy splajn svoje 
        }

        protected virtual Vector4[,] CreateInterpolationPoints(Vector4[,] Vector)
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
                MessageBox.Show("pozor, bod ma nulovu vahu");
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
            ErrorValues = new float[m, n];

            Vector3 S = Vector3.Zero;   //SplajnPoint
            float terrain_point_elevation;

            for (int i =0;i<m;i++)
            {
                for (int j =0;j<n;j++)
                {
                    S = Rational(InterpolationPoints[i, j]);
                    if (isRBF)
                    {
                        terrain_point_elevation = RefTerrain.GetRealZ(i,j); //same-indexed verices in both terrain and splajn have same x and y values
                    }
                    else
                    {
                        //with parametric surfaces, it is sometimes hard to find z for given x and y. It's easier to find approximate point on the real data = regular grid
                        terrain_point_elevation = RefTerrain.GetApproximateZver2(S.X, S.Y);
                    }

                    ErrorValues[i, j] = Math.Abs(S.Z - terrain_point_elevation);
                }
            }
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

        public float GetErrorValue(int i,int j)
        {
            if(ErrorValues != null)
            {
                if(0<=i && i<=m && 0<=j && j <=n)
                {
                    return ErrorValues[i, j];
                }
            }
            return 0;
        }
    }
}
