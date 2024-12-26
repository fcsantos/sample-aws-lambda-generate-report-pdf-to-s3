using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using System.Text.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace LambdaGeneratePDFReportToS3;

public class Function
{

    private readonly IAmazonS3 _s3Client;
    private const string OUTPUT_BUCKET = "bucket-sample-lambda-pdf";

    public Function()
    {
        _s3Client = new AmazonS3Client();
    }

    /// <summary>
    /// Handles the Lambda function execution to generate a PDF report and upload it to an S3 bucket.
    /// </summary>
    /// <param name="request">The request containing the report parameters such as start and end dates.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns>A JSON string containing a success message and a pre-signed URL for downloading the generated PDF report.</returns>
    /// <exception cref="Exception">Throws an exception if an error occurs during the report generation or upload process.</exception>
    public async Task<string> FunctionHandler(ReportRequest request, ILambdaContext context)
    {
        try
        {
            // Log da requisi��o
            context.Logger.LogInformation($"Processando relat�rio para o per�odo: {request.StartDate} at� {request.EndDate}");

            // Busca dados necess�rios (exemplo simulado)
            var dados = await BuscarDados(request.StartDate, request.EndDate);

            // Gera o PDF
            var pdfBytes = await GerarPDF(dados);

            // Nome do arquivo baseado na data
            string fileName = $"relatorio_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

            // Upload para S3
            await _s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = OUTPUT_BUCKET,
                Key = fileName,
                InputStream = new MemoryStream(pdfBytes)
            });

            // Gera URL pr�-assinada para download
            var urlRequest = new GetPreSignedUrlRequest
            {
                BucketName = OUTPUT_BUCKET,
                Key = fileName,
                Expires = DateTime.UtcNow.AddHours(1)
            };

            string downloadUrl = _s3Client.GetPreSignedURL(urlRequest);

            return JsonSerializer.Serialize(new
            {
                Message = "Relat�rio gerado com sucesso",
                DownloadUrl = downloadUrl
            });
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Erro ao gerar relat�rio: {ex.Message}");
            throw;
        }
    }

    private async Task<List<DadosVenda>> BuscarDados(DateTime startDate, DateTime endDate)
    {
        // Aqui voc� implementaria a l�gica para buscar dados do seu banco/origem
        // Exemplo simulado:
        return new List<DadosVenda>
        {
            new DadosVenda { Data = DateTime.Now, Produto = "Produto A", Quantidade = 10, ValorTotal = 1000 },
            new DadosVenda { Data = DateTime.Now.AddDays(-1), Produto = "Produto B", Quantidade = 5, ValorTotal = 500 }
        };
    }

    private async Task<byte[]> GerarPDF(List<DadosVenda> dados)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new PdfWriter(memoryStream);
        using var pdf = new PdfDocument(writer);
        using var document = new Document(pdf);

        // Adiciona t�tulo
        document.Add(new Paragraph("Relat�rio de Vendas")
            .SetFontSize(20));

        // Adiciona data de gera��o
        document.Add(new Paragraph($"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
            .SetFontSize(10));

        // Cria tabela
        var table = new Table(4).UseAllAvailableWidth();

        // Cabe�alho
        table.AddHeaderCell("Data");
        table.AddHeaderCell("Produto");
        table.AddHeaderCell("Quantidade");
        table.AddHeaderCell("Valor Total");

        // Dados
        foreach (var venda in dados)
        {
            table.AddCell(venda.Data.ToString("dd/MM/yyyy"));
            table.AddCell(venda.Produto);
            table.AddCell(venda.Quantidade.ToString());
            table.AddCell($"R$ {venda.ValorTotal:N2}");
        }

        document.Add(table);

        // Adiciona totais
        document.Add(new Paragraph($"Total de Vendas: R$ {dados.Sum(x => x.ValorTotal):N2}")
            .SetFontSize(12));

        document.Close();
        return memoryStream.ToArray();
    }
}