using MVFC.Veragi.Simulator.Shareable.Responses.Schedules;
using MVFC.Veragi.Simulator.Shareable.Responses.Webhooks;
using MVFC.Veragi.Simulator.Shareable.Enums;

namespace MVFC.Veragi.Simulator.Domain.Mappings;

public static class WebhookMappings
{
    public static ContractWebhookNotification ToContractReceivablesWebhook(this ScheduleQuery query) =>
        new(query.Status!.Value, query.Detail!, new ContractWebhookData(query.ScheduleQueryData!.RequestId!, query.ScheduleQueryData.OriginType!.Value, query.ScheduleQueryData.UpdatedAt!, query.ScheduleQueryData.MerchantCnpj!, Acquirers: query.ScheduleQueryData.Acquirers?.Select(a => new ContractWebhookAcquirer(a.Cnpj!, [.. a.PaymentArrangements!.Select(p => new ContractWebhookArrangement(p.Code!, [.. p.ReceivableUnits!.Select(u => new ContractWebhookUnit(u.SettlementDate!, u.TotalAmount!.Value, u.FreeAmount!.Value))]))])).ToArray()));

    public static ScheduleWebhookNotification ToScheduleWebhook(this ScheduleQuery query) => new()
    {
        Status = query.Status,
        Detalhe = query.Status == ScheduleQueryStatusType.PROCESSED ? "A solicitação de consulta de agenda foi processada com sucesso." : query.Status == ScheduleQueryStatusType.PROCESSING ? "A solicitação de consulta de agenda está em processamento." : "Não foi possível recuperar a agenda do registrador de recebíveis.",
        DadosConsultaAgenda = new ScheduleWebhookData(IdRequisicao: query.ScheduleQueryData!.RequestId, TipoOrigem: query.ScheduleQueryData.OriginType, DataHoraAtualizacao: query.ScheduleQueryData.UpdatedAt, CnpjEstabelecimento: query.ScheduleQueryData.MerchantCnpj, Credenciadoras: query.ScheduleQueryData.Acquirers?.Select(acquirer => new ScheduleWebhookPaymentAcquirer(Cnpj: acquirer.Cnpj, ArranjosPagamento: acquirer.PaymentArrangements?.Select(arrangement => new ScheduleWebhookPaymentArrangement(Codigo: arrangement.Code, UnidadesRecebiveis: arrangement.ReceivableUnits?.Select(unit => new ScheduleWebhookReceivableUnit(CnpjTitular: unit.HolderCnpj, DataVencimento: unit.SettlementDate, ValorTotal: unit.TotalAmount, ValorLivre: unit.FreeAmount)).ToList())).ToList())).ToList()),
    };
}
