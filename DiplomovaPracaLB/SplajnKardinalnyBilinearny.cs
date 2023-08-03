using OpenTK;


namespace DiplomovaPracaLB
{
    public class SplajnKardinalnyBilinearny : Splajn
    {
        float tension;

        public SplajnKardinalnyBilinearny(Vector4[,] Vstup, int _LOD, float _tension)
        {
            tension = (1 - _tension) / 2;
            LoadDimensions(_LOD, Vstup);
            Interpolate(Vstup);
        }

        protected override void LoadDimensions(int _Level_Of_Detail, Vector4[,] Vstup)
        {
            LOD = _Level_Of_Detail;
            m = (Vstup.GetLength(0) - 2 - 1) * (LOD + 1) + 1; //-2 krajne body z myslienky Anidiho twistov Coonsa odcitam od vstupnych 
            n = (Vstup.GetLength(1) - 2 - 1) * (LOD + 1) + 1;
        }

        protected override Vector4[,] CreateInterpolationPoints(Vector4[,] Vstup)
        {
            Vector4[,] IP = new Vector4[m, n];
            float s = tension;
            Matrix4 LocalControlPoints;

            //predvypocitam hodnoty hermitovych bazickych funkcii pre rovnomerne rozmiestnene body na krivke. budem ich pouzivat pri kazdej zaplatke
            Vector4[] Hu = new Vector4[LOD];    //v riadkoch su ulozene vycislene hodnoty 4 zmiesavacich fcii , v kazdom riadku je ina hodnota parametra, v kazdom stlpci rovnaka zmiesavacia fcia
            Matrix4 H = new Matrix4(0, 1, 0, 0, -s, 0, s, 0, 2 * s, s - 3, 3 - 2 * s, -s, -s, 2 - s, s - 2, s);
            for (int i = 1; i <= LOD; i++)    //0 a lOD+1 su okrajove, kt. pozname
            {
                float t = i / (float)(LOD + 1);   //parameter s je z <0,1>
                Vector4 u = new Vector4(1, t, t * t, t * t * t);
                Hu[i - 1] = MyMultiply(u, H);
            }

            IP[0, 0] = Vstup[1, 1]; //prvy bod interpolacie
            for (int j = 1; j < Vstup.GetLength(1) - 2; j++)
            {
                for (int i = 1; i < Vstup.GetLength(0) - 2; i++)
                {
                    int a = (i - 1) * (LOD + 1); //index laveho dolneho rohu v IntrepolacncyhBodoch
                    int b = (j - 1) * (LOD + 1);
                    //teraz vytvorím mini záplatu

                    //Okrajové body
                    Vector4 C00 = Vstup[i, j];
                    Vector4 C01 = Vstup[i, j + 1];
                    Vector4 C10 = Vstup[i + 1, j];
                    Vector4 C11 = Vstup[i + 1, j + 1];

                    //zapamatam si ich do vysledku

                    IP[a + LOD + 1, b] = C10;
                    IP[a, b + LOD + 1] = C01;
                    IP[a + LOD + 1, b + LOD + 1] = C11; //TODO toto je neefektivne, opakovane sa zapisuje rovnaka hodnota



                    //hodnoty Hermitovych krivkovych splajnov na okrajoch zaplatky, vypocitane v yjemnenych bodoch
                    for (int k = 0; k < LOD; k++)
                    {
                        //uplne prve krajne segmenty c0 a d0
                        if (j == 1)
                        {
                            LocalControlPoints = new Matrix4(Vstup[i - 1, j], Vstup[i, j], Vstup[i + 1, j], Vstup[i + 2, j]);
                            IP[a + k + 1, 0] = MyMultiply(Hu[k], LocalControlPoints);
                        }

                        if (i == 1)
                        {
                            LocalControlPoints = new Matrix4(Vstup[i, j - 1], Vstup[i, j], Vstup[i, j + 1], Vstup[i, j + 2]);
                            IP[0, b + k + 1] = MyMultiply(Hu[k], LocalControlPoints);
                        }

                        //vypocet segmentov c1,d1 (v dalsom kroku to budu c0, d0)
                        LocalControlPoints = new Matrix4(Vstup[i - 1, j + 1], Vstup[i, j + 1], Vstup[i + 1, j + 1], Vstup[i + 2, j + 1]);
                        IP[a + 1 + k, b + LOD + 1] = MyMultiply(Hu[k], LocalControlPoints);

                        LocalControlPoints = new Matrix4(Vstup[i + 1, j - 1], Vstup[i + 1, j], Vstup[i + 1, j + 1], Vstup[i + 1, j + 2]);
                        IP[a + LOD + 1, b + 1 + k] = MyMultiply(Hu[k], LocalControlPoints);
                    }


                    //Coonsova zaplata C0napojenie
                    // = zaplata s okraojvymi krivkami c0 c1 d0, d1 (oproti sebe rovnake picmenka)

                    //na Coonsovu zaplatu potrebujeme 3 podzaplaty:
                    //priamkova zaplata IZAP - interpoluje krivky C
                    //priamkova zaplata JZAP - interpoluje krivky D
                    //korekcna zaplata, ktora bilinearne interpoluje rohy zaplaty

                    for (int l = 1; l <= LOD; l++)
                    {
                        float q = l / (float)(LOD + 1);

                        for (int k = 1; k <= LOD; k++)
                        {
                            float p = k / (float)(LOD + 1);

                            Vector4 LinInterpJZap = (1 - p) * IP[a, b + l] + p * IP[a + LOD + 1, b + l];
                            Vector4 LinInterpIZap = (1 - q) * IP[a + k, b] + q * IP[a + k, b + LOD + 1];

                            Vector4 KorekcnaZap = (1 - p) * (1 - q) * C00 + p * (1 - q) * C10 + (1 - p) * q * C01 + p * q * C11;

                            IP[a + k, b + l] = LinInterpIZap + LinInterpJZap - KorekcnaZap;
                            //Console.WriteLine(IP[a+k, b+l]);
                        }
                    }
                }
            }

            return IP;
        }
    }
}
