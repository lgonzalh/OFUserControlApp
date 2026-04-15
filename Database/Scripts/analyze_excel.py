#!/usr/bin/env python3
"""
Script para analizar la estructura del archivo Excel HM_Listado_de_Usuarios...
y generar script SQL con los datos correctos.
"""

import pandas as pd
import sys
import os

def analyze_excel_file(file_path):
    """Analiza el archivo Excel y muestra su estructura"""
    try:
        # Leer el archivo Excel
        df = pd.read_excel(file_path)
        
        print(f"Archivo: {file_path}")
        print(f"Total de filas: {len(df)}")
        print(f"Total de columnas: {len(df.columns)}")
        print("\nColumnas encontradas:")
        for i, col in enumerate(df.columns, 1):
            print(f"{i}. '{col}' - Tipo: {df[col].dtype}")
        
        print("\nPrimeras 5 filas:")
        print(df.head())
        
        return df
        
    except Exception as e:
        print(f"Error al leer el archivo: {e}")
        return None

def generate_sql_script(df, table_name="Stg_UsuariosExcel"):
    """Genera script SQL para insertar los datos"""
    
    # Mapeo de columnas comunes - detectar columnas de usuario/correo
    column_mapping = {}
    
    # Buscar columnas que podrian ser "Usuario" o "Correo"
    for col in df.columns:
        col_lower = col.lower().strip()
        if 'usuario' in col_lower or 'user' in col_lower or 'login' in col_lower:
            column_mapping['Usuario'] = col
        elif 'correo' in col_lower or 'email' in col_lower or 'mail' in col_lower:
            column_mapping['Correo'] = col
    
    # Si no encontramos las columnas esperadas, usar las primeras columnas disponibles
    if 'Usuario' not in column_mapping and len(df.columns) >= 1:
        column_mapping['Usuario'] = df.columns[0]
        print(f"Advertencia: No se encontro columna 'Usuario'. Usando '{df.columns[0]}' como Usuario")
    
    if 'Correo' not in column_mapping and len(df.columns) >= 2:
        column_mapping['Correo'] = df.columns[1]
        print(f"Advertencia: No se encontro columna 'Correo'. Usando '{df.columns[1]}' como Correo")
    
    print(f"\nMapeo de columnas detectado:")
    for standard, actual in column_mapping.items():
        print(f"  {standard} -> {actual}")
    
    # Generar script SQL
    sql_script = f"""-- =============================================================
-- Script generado desde {os.path.basename(file_path)}
-- Fecha: {pd.Timestamp.now().strftime('%Y-%m-%d %H:%M:%S')}
-- Total de usuarios: {len(df)}
-- =============================================================

TRUNCATE TABLE "{table_name}";

-- Insercion de usuarios desde Excel
-- Columnas mapeadas automaticamente
"""
    
    # Generar inserts
    for index, row in df.iterrows():
        proceso_id = "gen_' || gen_random_uuid() || '"  # Usar funcion PostgreSQL
        usuario = str(row[column_mapping.get('Usuario', df.columns[0])]).replace("'", "''") if pd.notna(row[column_mapping.get('Usuario', df.columns[0])]) else ""
        correo = str(row[column_mapping.get('Correo', df.columns[1] if len(df.columns) > 1 else df.columns[0])]).replace("'", "''") if pd.notna(row[column_mapping.get('Correo', df.columns[1] if len(df.columns) > 1 else df.columns[0])]) else ""
        fuente_archivo = os.path.basename(file_path)
        
        sql_script += f"""INSERT INTO "{table_name}" ("ProcesoId", "Usuario", "Correo", "FuenteArchivo", "CargadoEn") VALUES
('{proceso_id}', '{usuario}', '{correo}', '{fuente_archivo}', CURRENT_TIMESTAMP);
"""
    
    sql_script += f"""
-- =============================================================
-- Verificacion de datos insertados
-- =============================================================
SELECT 
    COUNT(*) as TotalUsuarios,
    COUNT(CASE WHEN "Usuario" IS NOT NULL AND "Usuario" != '' THEN 1 END) as UsuariosConNombre,
    COUNT(CASE WHEN "Correo" IS NOT NULL AND "Correo" != '' THEN 1 END) as UsuariosConCorreo
FROM "{table_name}";
"""
    
    return sql_script

def main():
    # Buscar archivo Excel en el directorio
    script_dir = os.path.dirname(os.path.abspath(__file__))
    parent_dir = os.path.dirname(script_dir)
    
    # Buscar archivos Excel que comiencen con "HM_Listado_de_Usuarios"
    excel_files = []
    for file in os.listdir(parent_dir):
        if file.startswith("HM_Listado_de_Usuarios") and file.endswith(('.xlsx', '.xls')):
            excel_files.append(os.path.join(parent_dir, file))
    
    if not excel_files:
        print("No se encontraron archivos Excel que comiencen con 'HM_Listado_de_Usuarios'")
        print(f"Directorio buscado: {parent_dir}")
        return
    
    # Usar el primer archivo encontrado
    excel_file = excel_files[0]
    print(f"Analizando archivo: {excel_file}")
    
    # Analizar el archivo
    df = analyze_excel_file(excel_file)
    
    if df is not None:
        # Generar script SQL
        sql_script = generate_sql_script(df)
        
        # Guardar script SQL
        output_file = os.path.join(script_dir, "d_Insert_UsuariosExcel_FromHMListado.sql")
        with open(output_file, 'w', encoding='utf-8') as f:
            f.write(sql_script)
        
        print(f"\nScript SQL generado: {output_file}")
        print("\nResumen del script:")
        print(f"   - Tabla destino: Stg_UsuariosExcel")
        print(f"   - Total de registros: {len(df)}")
        print(f"   - Columnas mapeadas: {len(df.columns)}")

if __name__ == "__main__":
    main()