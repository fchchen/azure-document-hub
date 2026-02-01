import { Component, ViewChild } from '@angular/core';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTabsModule } from '@angular/material/tabs';
import { DocumentUploadComponent } from './components/document-upload/document-upload.component';
import { DocumentListComponent } from './components/document-list/document-list.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    MatToolbarModule,
    MatIconModule,
    MatButtonModule,
    MatTabsModule,
    DocumentUploadComponent,
    DocumentListComponent
  ],
  template: `
    <mat-toolbar color="primary">
      <mat-icon>cloud</mat-icon>
      <span class="app-title">Azure Document Hub</span>
      <span class="spacer"></span>
      <span class="subtitle">Demo Project</span>
    </mat-toolbar>

    <main class="main-content">
      <app-document-upload (uploaded)="onDocumentUploaded()"></app-document-upload>
      <app-document-list #documentList></app-document-list>
    </main>

    <footer>
      <p>Built with Angular 18+ | .NET 8 | Azure Functions | Cosmos DB | Blob Storage</p>
    </footer>
  `,
  styles: [`
    :host {
      display: flex;
      flex-direction: column;
      min-height: 100vh;
    }

    mat-toolbar {
      gap: 8px;
    }

    .app-title {
      font-weight: 500;
    }

    .spacer {
      flex: 1 1 auto;
    }

    .subtitle {
      font-size: 14px;
      opacity: 0.8;
    }

    .main-content {
      flex: 1;
      max-width: 1200px;
      margin: 0 auto;
      width: 100%;
      padding: 16px;
    }

    footer {
      text-align: center;
      padding: 16px;
      background-color: #f5f5f5;
      color: #666;
      font-size: 12px;
    }
  `]
})
export class App {
  @ViewChild('documentList') documentList!: DocumentListComponent;

  onDocumentUploaded() {
    this.documentList.loadDocuments();
  }
}
