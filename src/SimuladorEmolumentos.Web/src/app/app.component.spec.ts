import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AppComponent } from './app.component';

const catalog = [
  { state: 'SP', actId: 'certidao', actName: 'Certidão', requiresBase: false },
  { state: 'SP', actId: 'escritura', actName: 'Escritura', requiresBase: true }
];
describe('Simulador integrado ao HTTP', () => {
  let component: AppComponent;
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [AppComponent], providers: [provideHttpClient(), provideHttpClientTesting()] });
    component = TestBed.createComponent(AppComponent).componentInstance;
    http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/catalog').flush(catalog);
  });
  afterEach(() => http.verify());
  it('envia null para a tarifa fixa e recebe o resultado do servidor', () => {
    component.calculate();
    const request = http.expectOne('/api/simulations');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ state: 'SP', actId: 'certidao', baseAmount: null });
    request.flush({ ruleId: 'fixed', formula: 'Fixo', parameters: '', items: [], total: 35 });
    expect(component.result()?.total).toBe(35);
    component.form.controls.actId.setValue('escritura');
    expect(component.result()).toBeNull();
  });
  it('valida moeda brasileira antes de chamar a API', () => {
    component.form.controls.actId.setValue('escritura');
    for (const value of ['', '0', '-1', '10.00', '1.000,00', '1000000000,01', '1,001']) {
      component.form.controls.base.setValue(value);
      component.calculate();
      expect(component.form.invalid).toBe(true);
      http.expectNone('/api/simulations');
    }
    component.form.controls.base.setValue('100,50');
    component.calculate();
    const request = http.expectOne('/api/simulations');
    expect(request.request.body.baseAmount).toBe(100.5);
    request.flush({ ruleId: 'percent', formula: '1%', parameters: '', items: [], total: 1.01 });
    expect(component.result()?.total).toBe(1.01);
  });
  it('cancela cálculo pendente quando as entradas mudam', () => {
    component.calculate();
    const request = http.expectOne('/api/simulations');
    component.form.controls.actId.setValue('escritura');
    expect(request.cancelled).toBe(true);
    expect(component.busy()).toBe(false);
    expect(component.result()).toBeNull();
  });
  it('apresenta erro da API sem conservar total anterior', () => {
    component.calculate();
    http.expectOne('/api/simulations').flush({ detail: 'Regra indisponível.' }, { status: 400, statusText: 'Bad Request' });
    expect(component.error()).toBe('Regra indisponível.');
    expect(component.result()).toBeNull();
    expect(component.busy()).toBe(false);
  });
});
