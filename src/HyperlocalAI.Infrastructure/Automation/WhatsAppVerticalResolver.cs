using HyperlocalAI.Application;
using Microsoft.Extensions.Options;

namespace HyperlocalAI.Infrastructure.Automation;

public static class WhatsAppVerticalResolver
{
    public static BusinessVertical ResolveVertical(IOptions<WhatsAppOptions> options, string phoneNumberId)
    {
        var o = options.Value;
        if (o.PhoneNumberIdToVertical.TryGetValue(phoneNumberId, out var name)
            && Enum.TryParse<BusinessVertical>(name, true, out var v))
            return v;

        return Enum.TryParse<BusinessVertical>(o.DefaultVertical, true, out var d)
            ? d
            : BusinessVertical.Shop;
    }

    public static string BusinessName(IOptions<WhatsAppOptions> options, BusinessVertical vertical) =>
        vertical switch
        {
            BusinessVertical.Clinic => options.Value.ClinicBusinessName,
            BusinessVertical.RealEstate => options.Value.RealEstateBusinessName,
            _ => options.Value.ShopBusinessName,
        };
}
