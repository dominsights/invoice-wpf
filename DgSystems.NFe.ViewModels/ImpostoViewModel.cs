﻿using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using DgSystems.NFe.Core.Cadastro;
using DgSystems.NFe.ViewModels.Commands;
using GalaSoft.MvvmLight.Command;
using NFe.Core.Cadastro.Imposto;
using NFe.Core.Interfaces;
using NFe.Core.Messaging;
using NFe.Core.Utils;
using NFe.WPF.Events;
using NFe.WPF.ViewModel.Base;

namespace DgSystems.NFe.ViewModels
{
    public class ImpostoViewModel : ViewModelBaseValidation
    {
        private ObservableCollection<string> _cstList;

        public int Id { get; set; }
        public string CFOP { get; set; }
        public string Descricao { get; set; }

        public ICMS ICMS { get; set; }
        public ICMSST ICMSST { get; set; }
        public IPI IPI { get; set; }
        public PIS PIS { get; set; }
        public COFINS COFINS { get; set; }

        private bool _isShowRegime;
        private bool _isShowReducao;

        private Imposto _imposto;

        public Imposto Imposto
        {
            get { return _imposto; }
            set { SetProperty(ref _imposto, value); }
        }

        internal void AlterarImposto(GrupoImpostos obj)
        {
            Impostos.Clear();

            CFOP = obj.CFOP;
            Descricao = obj.Descricao;
            Id = obj.Id;

            foreach(var imposto in obj.Impostos)
            {
                Impostos.Add(new Imposto() { Aliquota = imposto.Aliquota, CST = imposto.CST, Nome = imposto.TipoImposto.ToString(), Id = imposto.Id });
            }

            var command = new OpenCadastroImpostoWindowCommand(this);
            MessagingCenter.Send(this, nameof(OpenCadastroImpostoWindowCommand), command);
        }

        private ObservableCollection<Imposto> _impostos;
        private readonly IGrupoImpostosRepository _grupoImpostosRepository;

        public ObservableCollection<Imposto> Impostos
        {
            get { return _impostos; }
            set { SetProperty(ref _impostos, value); }
        }

        public bool IsShowRegime
        {
            get { return _isShowRegime; }
            set { SetProperty(ref _isShowRegime, value); }
        }

        public bool IsShowReducao
        {
            get { return _isShowReducao; }
            set { SetProperty(ref _isShowReducao, value); }
        }

        public ObservableCollection<string> CstList
        {
            get { return _cstList; }
            set { SetProperty(ref _cstList, value); }
        }

        public ICommand SalvarCmd { get; set; }
        public ICommand CancelarCmd { set; get; }
        public ICommand ImpostoSelecionadoCmd { set; get; }
        public ICommand AdiciionarImpostoCmd { set; get; }
        public ICommand RemoverImpostoCmd { set; get; }

        public ImpostoViewModel(IGrupoImpostosRepository grupoImpostosRepository)
        {
            SalvarCmd = new RelayCommand<Window>(SalvarCmd_Execute, null);
            CancelarCmd = new RelayCommand<object>(CancelarCmd_Execute, null);
            ImpostoSelecionadoCmd = new RelayCommand(ImpostoSelecionadoCmd_Execute, null);
            AdiciionarImpostoCmd = new RelayCommand(AdiciionarImpostoCmd_Execute, null);
            RemoverImpostoCmd = new RelayCommand<Imposto>(RemoverImpostoCmd_Execute, null);

            ICMS = new ICMS();
            ICMSST = new ICMSST();
            IPI = new IPI();
            PIS = new PIS();
            COFINS = new COFINS();

            Imposto = new Imposto();
            Impostos = new ObservableCollection<Imposto>();
            _grupoImpostosRepository = grupoImpostosRepository;
        }

        private void RemoverImpostoCmd_Execute(Imposto imposto)
        {
            Impostos.Remove(imposto);
        }

        private void AdiciionarImpostoCmd_Execute()
        {
            Impostos.Add(Imposto);
            Imposto = new Imposto();
        }

        private void ImpostoSelecionadoCmd_Execute()
        {
            if (string.IsNullOrEmpty(Imposto.Nome))
            {
                return;
            }

            CstList = new ObservableCollection<string>(CstListManager.GetCstListPorImposto(Imposto.Nome));

            switch (Imposto.Nome)
            {
                case "PIS":
                case "COFINS":
                    IsShowRegime = true;
                    IsShowReducao = false;
                    break;

                case "ICMS":
                    IsShowReducao = true;
                    IsShowRegime = false;
                    break;

                default:
                    IsShowRegime = false;
                    IsShowReducao = false;
                    break;
            }
        }

        private void CancelarCmd_Execute(object obj)
        {
            throw new NotImplementedException();
        }

        private void SalvarCmd_Execute(Window window)
        {
            var grupoImpostos = new GrupoImpostos();

            foreach (var i in Impostos)
            {
                switch (i.Nome.ToUpperInvariant())
                {
                    case "ICMS":
                        grupoImpostos.Impostos.Add(new Core.Cadastro.Imposto() { CST = i.CST, Aliquota = i.Aliquota, TipoImposto = TipoImposto.Icms, Id = i.Id  });
                        break;

                    case "PIS":
                        grupoImpostos.Impostos.Add(new Core.Cadastro.Imposto() { CST = i.CST, Aliquota = i.Aliquota, TipoImposto = TipoImposto.PIS, Id = i.Id });
                        break;

                    case "COFINS":
                        grupoImpostos.Impostos.Add(new Core.Cadastro.Imposto() { CST = i.CST, Aliquota = i.Aliquota, TipoImposto = TipoImposto.Cofins, Id = i.Id });
                        break;

                    case "IPI":
                        grupoImpostos.Impostos.Add(new Core.Cadastro.Imposto() { CST = i.CST, Aliquota = i.Aliquota, TipoImposto = TipoImposto.IPI, Id = i.Id });
                        break;
                }
            }

            grupoImpostos.CFOP = CFOP;
            grupoImpostos.Descricao = Descricao;
            grupoImpostos.Id = Id;

            _grupoImpostosRepository.Salvar(grupoImpostos);

            var theEvent = new ImpostoAdicionadoEvent();
            MessagingCenter.Send(this, nameof(ImpostoAdicionadoEvent), theEvent);
            
            window.Close();
        }
    }
}
