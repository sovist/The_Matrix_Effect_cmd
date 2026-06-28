
namespace Matrix
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new MatrixConfig();

            var effect = new MatrixEffect(config);

            effect.Run();
        }
    }
}