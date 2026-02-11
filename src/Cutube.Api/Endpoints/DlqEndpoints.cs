using Cutube.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cutube.Api.Endpoints;

/// <summary>
/// Endpoints para gerenciamento da Dead Letter Queue.
/// </summary>
public static class DlqEndpoints
{
    public static void MapDlqEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/downloads/dlq")
            .WithTags("Dead Letter Queue");

        // GET /api/downloads/dlq - Listar mensagens na DLQ
        group.MapGet("/", async (
            DlqService dlqService,
            CancellationToken ct) =>
        {
            var messages = await dlqService.GetDeadLetterMessagesAsync(ct);

            return Results.Ok(new
            {
                TotalCount = messages.Count,
                Messages = messages.Select(m => new
                {
                    m.CorrelationId,
                    m.Url,
                    State = m.Status.ToString().ToLowerInvariant(),
                    m.ErrorMessage,
                    m.RetryCount,
                    m.CreatedAt,
                    m.UpdatedAt
                })
            });
        })
        .WithName("GetDlqMessages")
        .WithSummary("List all messages in Dead Letter Queue");

        // POST /api/downloads/dlq/{correlationId}/retry - Reprocessar mensagem
        group.MapPost("/{correlationId}/retry", async (
            string correlationId,
            DlqService dlqService,
            ILogger<Program> logger,
            CancellationToken ct) =>
        {
            try
            {
                var success = await dlqService.RetryAsync(correlationId, ct);

                if (!success)
                {
                    return Results.NotFound(new
                    {
                        error = "Message not found or not in DLQ state",
                        correlationId
                    });
                }

                logger.LogInformation(
                    "DLQ message retried: {CorrelationId}",
                    correlationId);

                return Results.Ok(new
                {
                    message = "Message requeued for processing",
                    correlationId,
                    status = "queued"
                });
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to retry DLQ message: {CorrelationId}",
                    correlationId);

                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Failed to retry message");
            }
        })
        .WithName("RetryDlqMessage")
        .WithSummary("Retry a message from Dead Letter Queue");

        // POST /api/downloads/dlq/retry-all - Reprocessar todas as mensagens
        group.MapPost("/retry-all", async (
            DlqService dlqService,
            ILogger<Program> logger,
            CancellationToken ct) =>
        {
            try
            {
                var result = await dlqService.RetryAllAsync(ct);

                logger.LogInformation(
                    "DLQ batch retry completed. Total: {Total}, Success: {Success}, Failed: {Failed}",
                    result.TotalCount, result.SuccessCount, result.FailedCount);

                return Results.Ok(new
                {
                    message = "Batch retry completed",
                    totalCount = result.TotalCount,
                    successCount = result.SuccessCount,
                    failedCount = result.FailedCount
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to execute batch retry");

                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Failed to retry messages");
            }
        })
        .WithName("RetryAllDlqMessages")
        .WithSummary("Retry all messages in Dead Letter Queue");

        // DELETE /api/downloads/dlq/{correlationId} - Descartar mensagem
        group.MapDelete("/{correlationId}", async (
            string correlationId,
            DlqService dlqService,
            ILogger<Program> logger,
            CancellationToken ct) =>
        {
            try
            {
                var success = await dlqService.DiscardAsync(correlationId, ct);

                if (!success)
                {
                    return Results.NotFound(new
                    {
                        error = "Message not found",
                        correlationId
                    });
                }

                logger.LogInformation(
                    "DLQ message discarded: {CorrelationId}",
                    correlationId);

                return Results.NoContent();
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to discard DLQ message: {CorrelationId}",
                    correlationId);

                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Failed to discard message");
            }
        })
        .WithName("DiscardDlqMessage")
        .WithSummary("Discard a message from Dead Letter Queue");
    }
}
