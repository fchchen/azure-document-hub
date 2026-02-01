import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { DocumentService } from '../../services/document.service';
import { Document } from '../../models/document.model';

@Component({
  selector: 'app-document-list',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    MatPaginatorModule,
    MatSnackBarModule,
    MatTooltipModule
  ],
  template: `
    <div class="document-list-container">
      @if (loading()) {
        <div class="loading-spinner">
          <mat-spinner diameter="40"></mat-spinner>
        </div>
      }

      <table mat-table [dataSource]="documents()" class="document-table">
        <ng-container matColumnDef="fileName">
          <th mat-header-cell *matHeaderCellDef>File Name</th>
          <td mat-cell *matCellDef="let doc">{{ doc.fileName }}</td>
        </ng-container>

        <ng-container matColumnDef="contentType">
          <th mat-header-cell *matHeaderCellDef>Type</th>
          <td mat-cell *matCellDef="let doc">{{ getFileTypeLabel(doc.contentType) }}</td>
        </ng-container>

        <ng-container matColumnDef="fileSize">
          <th mat-header-cell *matHeaderCellDef>Size</th>
          <td mat-cell *matCellDef="let doc">{{ formatFileSize(doc.fileSize) }}</td>
        </ng-container>

        <ng-container matColumnDef="status">
          <th mat-header-cell *matHeaderCellDef>Status</th>
          <td mat-cell *matCellDef="let doc">
            <mat-chip [class]="'status-' + doc.status.toLowerCase()">
              {{ doc.status }}
            </mat-chip>
          </td>
        </ng-container>

        <ng-container matColumnDef="createdAt">
          <th mat-header-cell *matHeaderCellDef>Uploaded</th>
          <td mat-cell *matCellDef="let doc">{{ doc.createdAt | date:'short' }}</td>
        </ng-container>

        <ng-container matColumnDef="actions">
          <th mat-header-cell *matHeaderCellDef>Actions</th>
          <td mat-cell *matCellDef="let doc">
            <button mat-icon-button matTooltip="Download" (click)="downloadDocument(doc)"
                    [disabled]="doc.status !== 'Completed'">
              <mat-icon>download</mat-icon>
            </button>
            <button mat-icon-button matTooltip="Delete" color="warn" (click)="deleteDocument(doc)">
              <mat-icon>delete</mat-icon>
            </button>
          </td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
        <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
      </table>

      @if (documents().length === 0 && !loading()) {
        <div class="no-documents">
          <mat-icon>folder_open</mat-icon>
          <p>No documents uploaded yet</p>
        </div>
      }

      <mat-paginator
        [length]="totalCount()"
        [pageSize]="pageSize()"
        [pageSizeOptions]="[5, 10, 25]"
        (page)="onPageChange($event)">
      </mat-paginator>
    </div>
  `,
  styles: [`
    .document-list-container {
      padding: 16px;
    }

    .document-table {
      width: 100%;
    }

    .loading-spinner {
      display: flex;
      justify-content: center;
      padding: 20px;
    }

    .no-documents {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 40px;
      color: #666;

      mat-icon {
        font-size: 48px;
        width: 48px;
        height: 48px;
      }
    }

    .status-pending {
      background-color: #fff3e0 !important;
    }

    .status-processing {
      background-color: #e3f2fd !important;
    }

    .status-completed {
      background-color: #e8f5e9 !important;
    }

    .status-failed {
      background-color: #ffebee !important;
    }
  `]
})
export class DocumentListComponent implements OnInit {
  private readonly documentService = inject(DocumentService);
  private readonly snackBar = inject(MatSnackBar);

  documents = signal<Document[]>([]);
  loading = signal(false);
  totalCount = signal(0);
  pageSize = signal(10);
  currentPage = signal(1);

  displayedColumns = ['fileName', 'contentType', 'fileSize', 'status', 'createdAt', 'actions'];

  ngOnInit() {
    this.loadDocuments();
  }

  loadDocuments() {
    this.loading.set(true);
    this.documentService.getDocuments(this.currentPage(), this.pageSize()).subscribe({
      next: (response) => {
        this.documents.set(response.documents);
        this.totalCount.set(response.totalCount);
        this.loading.set(false);
      },
      error: (err) => {
        this.snackBar.open('Failed to load documents', 'Close', { duration: 3000 });
        this.loading.set(false);
      }
    });
  }

  onPageChange(event: PageEvent) {
    this.currentPage.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
    this.loadDocuments();
  }

  downloadDocument(doc: Document) {
    this.documentService.getDownloadUrl(doc.id).subscribe({
      next: (response) => {
        window.open(response.downloadUrl, '_blank');
      },
      error: () => {
        this.snackBar.open('Failed to get download URL', 'Close', { duration: 3000 });
      }
    });
  }

  deleteDocument(doc: Document) {
    if (confirm(`Are you sure you want to delete "${doc.fileName}"?`)) {
      this.documentService.deleteDocument(doc.id).subscribe({
        next: () => {
          this.snackBar.open('Document deleted', 'Close', { duration: 3000 });
          this.loadDocuments();
        },
        error: () => {
          this.snackBar.open('Failed to delete document', 'Close', { duration: 3000 });
        }
      });
    }
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  getFileTypeLabel(contentType: string): string {
    const types: { [key: string]: string } = {
      'application/pdf': 'PDF',
      'image/jpeg': 'JPEG',
      'image/png': 'PNG',
      'image/gif': 'GIF',
      'application/msword': 'DOC',
      'application/vnd.openxmlformats-officedocument.wordprocessingml.document': 'DOCX'
    };
    return types[contentType] || contentType.split('/')[1]?.toUpperCase() || 'Unknown';
  }
}
