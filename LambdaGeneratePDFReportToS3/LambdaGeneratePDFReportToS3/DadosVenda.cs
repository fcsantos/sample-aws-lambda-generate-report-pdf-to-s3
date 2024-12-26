namespace LambdaGeneratePDFReportToS3
{
    public class DadosVenda
    {
        public DateTime Data { get; set; }
        public string Produto { get; set; }
        public int Quantidade { get; set; }
        public decimal ValorTotal { get; set; }
    }
}