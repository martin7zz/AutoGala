using AutoGala.views;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoGala.ViewModels
{
    public class MainWindowViewModel
    {
        public SectionViewModel SectionView { get; }
        public RebarViewModel RebarView { get; }
        public LoadViewModel LoadView { get; }

        public MainWindowViewModel(SectionViewModel sectionView, RebarViewModel rebarView, LoadViewModel loadView) 
        {
            SectionView = sectionView;
            RebarView = rebarView;
            LoadView = loadView;
        }
    }
}
