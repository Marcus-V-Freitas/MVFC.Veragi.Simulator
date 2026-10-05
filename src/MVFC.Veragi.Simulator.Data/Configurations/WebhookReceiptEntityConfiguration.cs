using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MongoDB.EntityFrameworkCore.Extensions;
using MVFC.Veragi.Simulator.Domain.Entities;

namespace MVFC.Veragi.Simulator.Data.Configurations;

public sealed class WebhookReceiptEntityConfiguration : IEntityTypeConfiguration<WebhookReceiptEntity>
{
    public void Configure(EntityTypeBuilder<WebhookReceiptEntity> builder)
    {
        builder.ToCollection("webhook_receipts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
    }
}
