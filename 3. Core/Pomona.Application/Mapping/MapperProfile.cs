using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Pomona.Domain.Entity;
using Pomona.Protos.Common;
using Pomona.Protos.Contract;
using Pomona.Protos.Inventory;
using System;

namespace Pomona.Application.Mapping
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            #region Contract
            #endregion
            CreateMap<ItemType, TypeProto>()
                .ForMember(dest => dest.Id, o => o.MapFrom(src => src.Id.ToString()))
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

            #region Common

            #endregion

            #region Parametric
            CreateMap<Brand, TypeProto>()
                .ForMember(dest => dest.Id, o => o.MapFrom(src => src.Id.ToString()));

            CreateMap<IdentificationType, TypeProto>()
                .ForMember(dest => dest.Id, o => o.MapFrom(src => src.Id.ToString()));

            CreateMap<Person, TypeProto>()
                .ForMember(dest => dest.Id, o => o.MapFrom(src => src.Id.ToString()))
                .ForMember(dest => dest.Name, o => o.MapFrom(src => src.FullName));
            #endregion

            #region Inventory


            CreateMap<Item, ItemProto>()
                .ForMember(dest => dest.DateOfEntry, o => o.MapFrom(src => Timestamp.FromDateTime((src.DateOfEntry).ToUniversalTime())))
                .ForMember(dest => dest.DateOfSale, o => o.MapFrom(src => src.DateOfSale != null ? Timestamp.FromDateTime(((DateTime)src.DateOfSale).ToUniversalTime()) : null))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Watch, WatchProto>()
                //.ForMember(dest => dest.Item, o => o.MapFrom(src => new ItemProto { Id = src.Item.Id }))
                .ForMember(dest => dest.Item, o => o.MapFrom(src => src.Item))
                .ForMember(dest => dest.Brand, o => o.MapFrom(src => src.Brand))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
            #endregion
        }
    }
}
