namespace ApproachR10Windows.R10Protocol;
public static class Bytes {
    public static byte[] ToByteArray(this string hex) { var a=new byte[hex.Length/2]; for(int i=0;i<hex.Length;i+=2)a[i/2]=Convert.ToByte(hex.Substring(i,2),16); return a; }
    public static string ToHexString(this byte[] bytes)=>BitConverter.ToString(bytes).Replace("-","");
    public static string ToHexString(this IEnumerable<byte> bytes)=>ToHexString(bytes.ToArray());
    public static byte[] Checksum(this IEnumerable<byte> bytes)=>Crc16.ComputeChecksum(bytes);
}
public static class Cobs {
    public static IEnumerable<byte> Encode(IEnumerable<byte> input){var r=new List<byte>();int di=0;byte d=1;foreach(var b in input){if(b!=0&&d<255){r.Add(b);d++;}else{r.Insert(di,d);di=r.Count;d=1;}}if(r.Count!=255&&r.Count>0)r.Insert(di,d);return r;}
    public static IEnumerable<byte> Decode(IEnumerable<byte> input){var a=input.ToArray();var r=new List<byte>();int i=0;while(i<a.Length){byte d=a[i];if(a.Length<i+d||d<1)return [];if(d>1)for(byte j=1;j<d;j++)r.Add(a[i+j]);i+=d;if(d<255&&i<a.Length)r.Add(0);}return r;}
}
public static class Crc16 { const ushort Polynomial=0xA001; static readonly ushort[] Table=Build(); static ushort[] Build(){var t=new ushort[256];for(ushort i=0;i<t.Length;i++){ushort v=0,x=i;for(byte j=0;j<8;j++){v=(ushort)(((v^x)&1)!=0?(v>>1)^Polynomial:v>>1);x>>=1;}t[i]=v;}return t;} public static byte[] ComputeChecksum(IEnumerable<byte> bytes){ushort crc=0;foreach(var b in bytes)crc=(ushort)((crc>>8)^Table[(byte)(crc^b)]);return BitConverter.GetBytes(crc);} }
