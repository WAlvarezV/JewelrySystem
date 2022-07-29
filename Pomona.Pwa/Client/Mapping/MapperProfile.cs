using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Pomona.Models.Models;
using Pomona.Protos.Contract;
using Pomona.Protos.Inventory;
using System;

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

            CreateMap<WatchModel, ItemProto>()
                .ForMember(dest => dest.DateOfEntry, opt => opt.MapFrom(src => Timestamp.FromDateTime(DateTime.SpecifyKind(src.DateOfEntry, DateTimeKind.Utc))))
                .ForMember(dest => dest.DateOfSale, opt => opt.MapFrom(src => src.DateOfSale != null ? Timestamp.FromDateTime(((DateTime)src.DateOfSale).ToUniversalTime()) : null))
                .ForMember(dest => dest.ItemTypeId, opt => opt.MapFrom(src => 5))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<JewelModel, ItemProto>()
                .ForMember(dest => dest.DateOfEntry, opt => opt.MapFrom(src => Timestamp.FromDateTime(DateTime.SpecifyKind(src.DateOfEntry, DateTimeKind.Utc))))
                .ForMember(dest => dest.DateOfSale, opt => opt.MapFrom(src => src.DateOfSale != null ? Timestamp.FromDateTime(((DateTime)src.DateOfSale).ToUniversalTime()) : null))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));



            CreateMap<ItemProto, ItemModel>()
                .ForMember(dest => dest.DateOfEntry, opt => opt.MapFrom(src => src.DateOfEntry.ToDateTime()))
                .ForMember(dest => dest.DateOfSale, opt => opt.MapFrom(src => src.DateOfSale != null ? src.DateOfSale.ToDateTime() : DateTime.MinValue))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}
