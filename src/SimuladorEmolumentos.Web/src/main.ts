import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient } from '@angular/common/http';
import { LOCALE_ID } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import pt from '@angular/common/locales/pt';
import { AppComponent } from './app/app.component';
registerLocaleData(pt);
bootstrapApplication(AppComponent, { providers: [provideHttpClient(), { provide: LOCALE_ID, useValue: 'pt-BR' }] }).catch(console.error);
