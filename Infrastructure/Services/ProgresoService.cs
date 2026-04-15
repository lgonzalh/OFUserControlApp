using Microsoft.AspNetCore.SignalR;
using OFUserControlApp.Application.DTOs;
using OFUserControlApp.Application.Interfaces;
using OFUserControlApp.Domain.Entities;
using System.Collections.Concurrent;

namespace OFUserControlApp.Infrastructure.Services;

/// <summary>
/// Servicio para manejo de progreso en tiempo real
/// </summary>
public sealed class ProgresoService : IProgresoService
{
    private readonly IHubContext<ProgresoHub> _hubContext;
    private readonly ILogger<ProgresoService> _logger;
    private readonly ConcurrentDictionary<string, ProgresoInfo> _progressCache = new();
    private readonly ConcurrentDictionary<string, List<UsuarioResultadoDTO>> _resultadosCache = new();

    public ProgresoService(IHubContext<ProgresoHub> hubContext, ILogger<ProgresoService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyProgressAsync(string procesoId, int porcentaje, string etapa, string mensaje)
    {
        var progreso = new ProgresoInfo
        {
            ProcesoId = procesoId,
            Porcentaje = Math.Max(0, Math.Min(100, porcentaje)),
            Etapa = etapa,
            Mensaje = mensaje
        };

        SetProgreso(procesoId, progreso);

        try
        {
            await _hubContext.Clients.Group($"proceso_{procesoId}")
                .SendAsync("ActualizarProgreso", progreso);
            
            _logger.LogDebug("Progreso notificado para proceso {ProcesoId}: {Porcentaje}% - {Etapa}", 
                procesoId, porcentaje, etapa);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notificando progreso para proceso {ProcesoId}", procesoId);
        }
    }

    public void SetProgreso(string procesoId, ProgresoInfo progreso)
    {
        _progressCache.AddOrUpdate(procesoId, progreso, (key, oldValue) => progreso);
    }

    public ProgresoInfo? GetProgreso(string procesoId)
    {
        return _progressCache.TryGetValue(procesoId, out var progreso) ? progreso : null;
    }

    public async Task StoreResultadosAsync(string procesoId, List<UsuarioResultadoDTO> resultados)
    {
        _resultadosCache.AddOrUpdate(procesoId, resultados, (key, oldValue) => resultados);
        _logger.LogInformation("Resultados almacenados en memoria para proceso {ProcesoId}: {Count} registros", 
            procesoId, resultados.Count);
        await Task.CompletedTask;
    }

    public async Task<List<UsuarioResultadoDTO>?> GetResultadosAsync(string procesoId)
    {
        var resultado = _resultadosCache.TryGetValue(procesoId, out var resultados) ? resultados : null;
        _logger.LogDebug("Obteniendo resultados desde memoria para proceso {ProcesoId}: {Count} registros", 
            procesoId, resultado?.Count ?? 0);
        return await Task.FromResult(resultado);
    }
}

/// <summary>
/// Hub de SignalR para notificaciones en tiempo real
/// </summary>
public sealed class ProgresoHub : Hub
{
    private readonly ILogger<ProgresoHub> _logger;

    public ProgresoHub(ILogger<ProgresoHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinGroup(string procesoId)
    {
        var groupName = $"proceso_{procesoId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("Cliente {ConnectionId} se unió al grupo {GroupName}", Context.ConnectionId, groupName);
    }

    public async Task LeaveGroup(string procesoId)
    {
        var groupName = $"proceso_{procesoId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("Cliente {ConnectionId} salió del grupo {GroupName}", Context.ConnectionId, groupName);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogDebug("Cliente {ConnectionId} desconectado", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
