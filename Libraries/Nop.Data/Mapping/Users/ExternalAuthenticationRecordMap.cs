using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Users;

namespace Nop.Data.Mapping.Users
{
    /// <summary>
    /// Represents an external authentication record mapping configuration
    /// </summary>
    public partial class ExternalAuthenticationRecordMap : NopEntityTypeConfiguration<ExternalAuthenticationRecord>
    {
        #region Methods

        /// <summary>
        /// Configures the entity
        /// </summary>
        /// <param name="builder">The builder to be used to configure the entity</param>
        public override void Configure(EntityTypeBuilder<ExternalAuthenticationRecord> builder)
        {
            builder.ToTable(nameof(ExternalAuthenticationRecord));
            builder.HasKey(record => record.Id);

            base.Configure(builder);
        }

        #endregion
    }
}