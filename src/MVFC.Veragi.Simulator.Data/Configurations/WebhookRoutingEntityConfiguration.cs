using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MongoDB.EntityFrameworkCore.Extensions;
using MVFC.Veragi.Simulator.Domain.Entities;

namespace MVFC.Veragi.Simulator.Data.Configurations;

public sealed class WebhookRoutingEntityConfiguration : IEntityTypeConfiguration<WebhookRoutingEntity>
{
    public void Configure(EntityTypeBuilder<WebhookRoutingEntity> builder)
    {
        builder.ToCollection("webhook_routing");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
    }
}
