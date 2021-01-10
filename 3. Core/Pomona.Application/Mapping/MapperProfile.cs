using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Pomona.Domain.Entity;
using Pomona.Protos;

namespace Pomona.Application.Mapping
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<ItemType, ItemTypeProto>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<ContractProto, Person>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<ContractProto, Contract>()
                .ForMember(dest => dest.Date, o => o.MapFrom(src => src.Date.ToDateTime()))
                .ForMember(dest => dest.DeliveryDate, o => o.MapFrom(src => src.DeliveryDate.ToDateTime()))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Contract, ContractProto>()
                .ForMember(dest => dest.Date, o => o.MapFrom(src => Timestamp.FromDateTime((src.Date).ToUniversalTime())))
                .ForMember(dest => dest.DeliveryDate, o => o.MapFrom(src => Timestamp.FromDateTime((src.DeliveryDate).ToUniversalTime())))
                .ForMember(dest => dest.IdentificationTypeId, o => o.MapFrom(src => src.Person.IdentificationTypeId))
                .ForMember(dest => dest.IdentificationNumber, o => o.MapFrom(src => src.Person.IdentificationNumber))
                .ForMember(dest => dest.FullName, o => o.MapFrom(src => src.Person.FullName))
                .ForMember(dest => dest.CellPhone, o => o.MapFrom(src => src.Person.CellPhone))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Payment, PaymentProto>()
                .ForMember(dest => dest.Date, o => o.MapFrom(src => Timestamp.FromDateTime((src.Date).ToUniversalTime())))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}
