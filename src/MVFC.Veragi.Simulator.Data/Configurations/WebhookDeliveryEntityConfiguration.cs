using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MongoDB.EntityFrameworkCore.Extensions;
using MVFC.Veragi.Simulator.Domain.Entities;

namespace MVFC.Veragi.Simulator.Data.Configurations;

public sealed class WebhookDeliveryEntityConfiguration : IEntityTypeConfiguration<WebhookDeliveryEntity>
{
    public void Configure(EntityTypeBuilder<WebhookDeliveryEntity> builder)
    {
        builder.ToCollection("webhook_deliveries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
    }
}
