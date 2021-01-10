using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Pomona.Models.Models;
using Pomona.Protos;

namespace Pomona.Pwa.Client
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<ContractModel, ContractProto>()
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => Timestamp.FromDateTime(src.Date.ToUniversalTime())))
                .ForMember(dest => dest.DeliveryDate, opt => opt.MapFrom(src => Timestamp.FromDateTime(src.DeliveryDate.ToUniversalTime())))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<ContractProto, ContractModel>()
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.Date.ToDateTime()))
                .ForMember(dest => dest.DeliveryDate, opt => opt.MapFrom(src => src.DeliveryDate.ToDateTime()))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}
