using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using QuestPDF.ExampleInvoice;
using QuestPDF.Fluent;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/comprobantes")]
public class ComprobantesController : ControllerBase
{

    private static readonly string endpoint = "placeholder";
    private static readonly string accessKey = "placeholder";
    private static readonly string secretKey = "placeholder";
    private static readonly bool secure = true;

    private static readonly IMinioClient minio = new MinioClient()
        .WithEndpoint(endpoint)
        .WithCredentials(accessKey, secretKey)
        .WithSSL(secure)
        .Build();

    [HttpPost(Name = "descargarpdf")]
    public async Task<IActionResult> Post([FromBody] FinancieroDataModel data)
    {
        try
        {
            var getListBucketsTask = await minio.ListBucketsAsync();

            foreach (var bucket in getListBucketsTask.Buckets)
            {
                Console.WriteLine(bucket.Name + " " + bucket.CreationDateDateTime);
            }

            var model = InvoiceDocumentDataSource.GetInvoiceDetails();
            var document = new InvoiceDocument(model, data);

            // Generate PDF file
            byte[] pdfBytes = document.GeneratePdf();

            // Upload the PDF file to Minio
            var bucketName = "comprobantes";
            var objectName = "path/to/your/file.pdf";

            using (var stream = new MemoryStream(pdfBytes))
            {
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithObjectSize(pdfBytes.Length)
                    .WithStreamData(stream)
                    .WithContentType("application/pdf");

                await minio.PutObjectAsync(putObjectArgs);
                Console.WriteLine("Successfully uploaded " + objectName);
            }

            return Ok("PDF uploaded successfully");
        }
        catch (Exception ex)
        {
            // Handle exceptions appropriately
            Console.WriteLine($"Error uploading file to Minio: {ex.Message}");
            return StatusCode(500, "Internal Server Error");
        }
    }
}