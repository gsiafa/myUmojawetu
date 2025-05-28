namespace WebOptimus.Configuration
{
    using WebOptimus.Models;
     using AutoMapper;
    using WebOptimus.Models.ViewModel;

    public class MapperConfig : Profile 
    {
        public MapperConfig()
        {
           
            CreateMap<User, RegisterViewModel>().ReverseMap();
            CreateMap<User, EditViewModel>().ReverseMap();
            CreateMap<User, ResetPasswordViewModel>().ReverseMap();
            CreateMap<User, ProfileViewModel>().ReverseMap();
            CreateMap<User, ProfileEditViewModel>().ReverseMap();
            CreateMap<User, AccountViewModel>()
              .ForMember(dest => dest.DeactivateAccountViewModel, opt => opt.MapFrom(src => new DeactivateAccountViewModel
              {
                  PersonRegNumber = src.PersonRegNumber,
                  UserId = src.UserId
              }))
              .ReverseMap();

            CreateMap<User, ChangePasswordViewModel>().ReverseMap();
            CreateMap<AboutUser, AboutUserViewModel>().ReverseMap();         
            CreateMap<Spouse, SpouseViewModels>().ReverseMap();
            CreateMap<NextOfKin, NextOfKinViewModels>().ReverseMap();
            CreateMap<Announcement, AnnouncementViewModel>().ReverseMap();
            CreateMap<Donor, DonorViewModel>().ReverseMap();
            CreateMap<ReportedDeath, ReportedDeathViewModel>().ReverseMap();
            CreateMap<Gender, GenderViewModel>().ReverseMap();

            CreateMap<Cause, CauseViewModel>().ReverseMap();
            CreateMap<PopUpNotification, PopUpNotificationViewModel>().ReverseMap();
            CreateMap<Dependant, DependantViewModel>().ReverseMap();


            CreateMap<Region, RegionViewModel>().ReverseMap();

            CreateMap<Title, TitleViewModel>().ReverseMap();


            CreateMap<City, CityViewModel>().ReverseMap();
            CreateMap<Banner, BannerViewModel>().ReverseMap();
            CreateMap<DonationForNonDeathRelated, OtherDonationViewModel>().ReverseMap();

            CreateMap<DonationForNonDeathRelated, OtherDonationDetailsViewModel>().ReverseMap();

        }
    }
}
