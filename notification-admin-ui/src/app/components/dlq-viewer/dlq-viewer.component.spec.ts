import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DlqViewerComponent } from './dlq-viewer.component';

describe('DlqViewerComponent', () => {
  let component: DlqViewerComponent;
  let fixture: ComponentFixture<DlqViewerComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DlqViewerComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DlqViewerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
