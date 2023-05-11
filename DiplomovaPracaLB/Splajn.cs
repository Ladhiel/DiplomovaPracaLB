using System.Windows;
using OpenTK;


namespace DiplomovaPracaLB
{
    public abstract partial class Splajn
    {
        protected int LOD;
        protected int m, n; //pocet vrcholov v zjemnenom vzorkovani      indexy od 0 po m-1
        public Vector4[,] InterpolationPoints;
        public Vector3[,] Normals;              //normalove vektory v lavych dolnych rohov jemneho vzorkovania

        protected void Interpolate(Vector4[,] Vstup)
        {
            InterpolationPoints = CreateInterpolationPoints(Vstup);
            ComputeNormals(InterpolationPoints);
        }
        public void New(Vector4[,] Vstup)
        {
            Interpolate(Vstup);
        }

        public void AdjustLOD(Vector4[,] Vstup, int new_LOD)
        {
            LOD = new_LOD;
            LoadDimensions(Vstup);
            Interpolate(Vstup);
        }

        protected virtual void LoadDimensions(Vector4[,] Vstup)
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

        
    }
}
